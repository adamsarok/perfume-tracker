using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using PerfumeTracker.Evals.Recording;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.ChatAgent.Services;
using PerfumeTracker.Server.Features.Embedding;

namespace PerfumeTracker.Evals.Harness;

/// <summary>
/// Boots the real server in-process (same fixture pattern as PerfumeTracker.xTests), pointed at the
/// test database, with background workers disabled and the model client swapped for the eval client.
/// </summary>
public sealed class EvalHost : IAsyncDisposable {
	private readonly WebApplicationFactory<Program> factory;

	public EvalTenantProvider TenantProvider { get; } = new();
	public EvalResponsesClient ResponsesClient { get; }
	public Guid UserId { get; private set; }
	public int MaxIterations { get; private set; }
	public int MaxResultsPerToolCall { get; private set; }
	public bool MarketplaceAvailable { get; private set; }
	public string[] RegisteredToolNames { get; private set; } = [];

	public EvalHost(EvalRunOptions options, EvalRecordingStore store) {
		ResponsesClient = new EvalResponsesClient(options, store);
		// Minimal-host startup reads these values before ConfigureAppConfiguration callbacks run.
		var secrets = new ConfigurationBuilder().AddUserSecrets<Program>(optional: true).Build();
		var apiKey = options.ApiKey
			?? Environment.GetEnvironmentVariable("OpenAI__ApiKey")
			?? secrets["OpenAI:ApiKey"];
		var model = options.Model
			?? Environment.GetEnvironmentVariable("OpenAI__AssistantModel")
			?? secrets["OpenAI:AssistantModel"];

		factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => {
			// "Test" points the host at ConnectionStrings:PerfumeTrackerTest (see Program.cs).
			builder.UseEnvironment("Test");
			if (!string.IsNullOrWhiteSpace(apiKey)) builder.UseSetting("OpenAI:ApiKey", apiKey);
			if (!string.IsNullOrWhiteSpace(model)) builder.UseSetting("OpenAI:AssistantModel", model);
			if (options.Mode == EvalMode.Replay) {
				// Replay never talks to OpenAI, but the container still needs the registrations that
				// Program.cs gates on a configured key (ChatClient lives inside PerfumeRecommender).
				builder.UseSetting("OpenAI:ApiKey", "eval-replay-placeholder");
				builder.UseSetting("OpenAI:AssistantModel", "eval-replay-placeholder");
			}

			builder.ConfigureServices(services => {
				// Background workers would mutate the seeded fixture (embeddings, tag backfill, matcher) mid-run.
				services.RemoveAll<IHostedService>();

				services.RemoveAll<ITenantProvider>();
				services.AddScoped<ITenantProvider>(_ => TenantProvider);

				services.RemoveAll<IEncoder>();
				services.AddSingleton<IEncoder, NullEncoder>();

				services.RemoveAll<IResponsesClient>();
				services.RemoveAll<ResponsesClientAdapter>();
				services.AddSingleton<ResponsesClientAdapter>();
				services.AddSingleton<IResponsesClient>(ResponsesClient);
			});
		});
	}

	public string? ConfigurationValue(string key) => factory.Services.GetRequiredService<IConfiguration>()[key];

	public IServiceScope CreateScope() => factory.Services.CreateScope();

	public async Task InitializeAsync(CancellationToken cancellationToken) {
		ResponsesClient.InnerServices = factory.Services;

		using (var scope = factory.Services.CreateScope()) {
			UserId = await EvalDataSeeder.EnsureUserAsync(scope.ServiceProvider, cancellationToken);
		}
		TenantProvider.CurrentUserId = UserId;

		using (var scope = factory.Services.CreateScope()) {
			var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			await EvalDataSeeder.ResetAndSeedAsync(context, UserId, cancellationToken);

			var chatAgentOptions = scope.ServiceProvider.GetRequiredService<IOptions<ChatAgentOptions>>().Value;
			MaxIterations = Math.Max(1, chatAgentOptions.MaxIterations);
			MaxResultsPerToolCall = Math.Max(1, chatAgentOptions.MaxResultsPerToolCall);

			var tools = scope.ServiceProvider.GetRequiredService<IChatAgentTools>();
			RegisteredToolNames = tools.Tools.Select(tool => tool.FunctionName).ToArray();
			MarketplaceAvailable = tools.HasMarketplaceOffers;
		}
	}

	/// <summary>
	/// Hash of the system prompt + tool schemas. Recordings are namespaced by this value, so a
	/// prompt or tool change makes old recordings stale instead of silently passing.
	/// </summary>
	public async Task<string> ComputePromptFingerprintAsync(CancellationToken cancellationToken) {
		using var scope = factory.Services.CreateScope();
		var prompt = await scope.ServiceProvider.GetRequiredService<IChatSystemPromptBuilder>().BuildAsync(cancellationToken);
		var tools = scope.ServiceProvider.GetRequiredService<IChatAgentTools>().Tools;

		// The favourite-tags section comes from an unordered query, so normalise line order first.
		var normalisedPrompt = string.Join("\n", prompt
			.Split('\n')
			.Select(line => line.TrimEnd())
			.OrderBy(line => line, StringComparer.Ordinal));
		var toolSignature = string.Join("\n", tools
			.OrderBy(tool => tool.FunctionName, StringComparer.Ordinal)
			.Select(tool => $"{tool.FunctionName}|{tool.FunctionDescription}|{tool.FunctionParameters}"));

		return Fingerprints.Compute(normalisedPrompt + "\n###TOOLS###\n" + toolSignature);
	}

	public async Task<Dictionary<Guid, string>> GetOwnedPerfumeNamesAsync(CancellationToken cancellationToken) {
		using var scope = factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var ids = await context.Perfumes.AsNoTracking().Select(p => p.Id).ToListAsync(cancellationToken);
		return ids.ToDictionary(id => id, EvalWardrobe.DisplayNameForPerfumeId);
	}

	public async Task<ConversationSnapshot> ReadConversationAsync(Guid conversationId, CancellationToken cancellationToken) {
		using var scope = factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var messages = await context.ChatMessages
			.AsNoTracking()
			.IgnoreQueryFilters()
			.Where(message => message.ConversationId == conversationId)
			.OrderBy(message => message.MessageIndex)
			.ToListAsync(cancellationToken);
		var conversation = await context.ChatConversations
			.AsNoTracking()
			.IgnoreQueryFilters()
			.FirstOrDefaultAsync(item => item.Id == conversationId, cancellationToken);

		return new ConversationSnapshot(messages, conversation?.Title, conversation?.DiscussedPerfumeIds ?? []);
	}

	public async ValueTask DisposeAsync() => await factory.DisposeAsync();
}
