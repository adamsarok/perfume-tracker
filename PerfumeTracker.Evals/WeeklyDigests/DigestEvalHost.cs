using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PerfumeTracker.Evals.Harness;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.Evals.WeeklyDigests;

// Deliberately constructs only the agent and its dependencies: no server or background workers.
public sealed class DigestEvalHost : IDisposable {
	private readonly DbContextOptions<PerfumeTrackerContext> dbOptions;
	private readonly HttpClient http = new() { BaseAddress = new Uri("https://api.openai.com/v1/"), Timeout = TimeSpan.FromMinutes(30) };
	private readonly EvalRunOptions options;
	private readonly WeeklyDigestOptions retryOptions;
	public string? ApiKey { get; }
	public string Model { get; }

	public DigestEvalHost(EvalRunOptions options) {
		this.options = options;
		var server = Path.Combine(Path.GetDirectoryName(EvalPaths.ProjectDirectory)!, "PerfumeTracker.Server");
		var config = new ConfigurationBuilder().AddJsonFile(Path.Combine(server, "appsettings.json"), optional: true)
			.AddJsonFile(Path.Combine(server, "appsettings.Test.json"), optional: true)
			.AddUserSecrets<Program>(optional: true).AddEnvironmentVariables().Build();
		retryOptions = config.GetSection("WeeklyDigest").Get<WeeklyDigestOptions>() ?? new();
		var connection = config.GetConnectionString("PerfumeTrackerTest") ?? throw new InvalidOperationException("Configure ConnectionStrings:PerfumeTrackerTest.");
		ValidateConnection(connection);
		dbOptions = new DbContextOptionsBuilder<PerfumeTrackerContext>().UseNpgsql(connection, pg => pg.UseVector()).Options;
		ApiKey = options.ApiKey ?? config["OpenAI:ApiKey"];
		Model = options.Model ?? config["WeeklyDigest:Model"] ?? config["OpenAI:AssistantModel"] ?? throw new InvalidOperationException("Configure a digest model or pass --model.");
		if (options.Mode != EvalMode.Replay && string.IsNullOrWhiteSpace(ApiKey)) throw new InvalidOperationException("Configure OpenAI:ApiKey in server User Secrets or the environment.");
	}

	public static void ValidateConnection(string connection) {
		if (new Npgsql.NpgsqlConnectionStringBuilder(connection).Database?.Contains("test", StringComparison.OrdinalIgnoreCase) != true)
			throw new InvalidOperationException("Digest evals require a dedicated database with 'test' in its name.");
	}

	public async Task InitializeAsync(CancellationToken ct) {
		await using var db = new PerfumeTrackerContext(dbOptions, new EvalTenantProvider());
		await db.Database.MigrateAsync(ct);
	}

	public async Task<DigestEvalTrajectory> RunAsync(DigestEvalCase evalCase, string recordings, CancellationToken ct) {
		var fixture = DigestEvalFixtures.Build(evalCase);
		var trajectory = new DigestEvalTrajectory();
		var watch = Stopwatch.StartNew();
		var run = new WeeklyDigest { Id = DigestEvalFixtures.Id($"{evalCase.Id}:run"), UserId = fixture.UserId, WeekStart = fixture.WeekStart, Model = Model };
		try {
			await using var db = new PerfumeTrackerContext(dbOptions, new EvalTenantProvider { CurrentUserId = fixture.UserId });
			await DigestEvalFixtures.SeedAsync(db, fixture, ct);
			var source = new TracedDigestCandidates(new DigestCandidates(db, new FrozenDigestTime(evalCase.Now)), trajectory);
			var budget = evalCase.Budget;
			budget.MaxGenerationRetries = retryOptions.MaxGenerationRetries;
			budget.RetryBaseDelaySeconds = retryOptions.RetryBaseDelaySeconds;
			budget.RetryMaxDelaySeconds = retryOptions.RetryMaxDelaySeconds;
			if (!budget.IsValid()) throw new InvalidOperationException("Invalid weekly digest retry settings.");
			// Transport retry settings do not change the model's request contract. Preserve existing
			// fingerprints and record individual failures instead, including retained token allowances.
			var manifest = JsonSerializer.Serialize(new { fixture, options = new { budget.MaxTokens, budget.FinalizationReserve,
				budget.MaxOutputTokens, budget.MaxExplorationRequests, budget.MaxAttempts, budget.Model }, profile = new { minimumRating = 3, dayFilter = 90 } }, DigestJson.Options);
			IDigestModel? live = options.Mode == EvalMode.Replay ? null : new DigestModel(http, Options.Create(new OpenAIOptions { ApiKey = ApiKey! }));
			var model = new RecordedDigestModel(options.Mode, recordings, evalCase.Id, manifest, live, trajectory);
			if (options.Mode == EvalMode.Replay) { budget.RetryBaseDelaySeconds = 0; budget.RetryMaxDelaySeconds = 0; }
			var agent = new WeeklyDigestAgent(source, model, Options.Create(budget), NullLogger<WeeklyDigestAgent>.Instance);
			trajectory.Payload = await agent.Run(run, token => {
				token.ThrowIfCancellationRequested();
				trajectory.Saves.Add(new(run.TokensUsed, run.Checkpoint));
				return Task.CompletedTask;
			}, ct);
		} catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
		catch (Exception ex) { trajectory.Error = ex.Message; }
		trajectory.TokensUsed = run.TokensUsed;
		trajectory.Checkpoint = run.Checkpoint == "{}" ? null : JsonSerializer.Deserialize<DigestCheckpoint>(run.Checkpoint, DigestJson.Options);
		trajectory.DurationMs = watch.ElapsedMilliseconds;
		return trajectory;
	}

	public void Dispose() => http.Dispose();
}

public sealed class TracedDigestCandidates(IDigestCandidates inner, DigestEvalTrajectory trajectory) : IDigestCandidates {
	public async Task<object> GetContext(DateTime weekStart, CancellationToken ct) {
		var context = await inner.GetContext(weekStart, ct);
		trajectory.Context = JsonSerializer.SerializeToElement(context, DigestJson.Options);
		return context;
	}
	public async Task<DigestCandidatePage> Get(RecommendationStrategy category, int offset, int count, string? search, CancellationToken ct) {
		var page = await inner.Get(category, offset, count, search, ct);
		trajectory.CandidateCalls.Add(new(category.ToString(), offset, count, search, page));
		return page;
	}
}
