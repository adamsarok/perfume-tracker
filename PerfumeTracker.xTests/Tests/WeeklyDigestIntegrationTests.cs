using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.WeeklyDigests;
using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Weekly digest database")]
public class WeeklyDigestDatabaseCollection : ICollectionFixture<WeeklyDigestDatabase>;

public class WeeklyDigestDatabase : IAsyncLifetime {
	public WebApplication App { get; private set; } = null!;
	public DigestTestModel Model { get; } = new();
	private readonly List<Guid> createdUsers = [];

	public async ValueTask InitializeAsync() {
		var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
			"../../../../PerfumeTracker.Server/appsettings.Test.json")), optional: true).AddEnvironmentVariables().Build();
		var connection = config.GetConnectionString("PerfumeTrackerTest");
		if (string.IsNullOrWhiteSpace(connection)) throw new InvalidOperationException("Configure ConnectionStrings__PerfumeTrackerTest or ConnectionStrings:PerfumeTrackerTest in appsettings.Test.json to use a dedicated test database.");
		var databaseName = new Npgsql.NpgsqlConnectionStringBuilder(connection).Database;
		if (databaseName?.Contains("test", StringComparison.OrdinalIgnoreCase) != true) throw new InvalidOperationException("A dedicated test database is required.");
		var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Test" });
		builder.Logging.ClearProviders().AddConsole();
		builder.WebHost.UseTestServer();
		builder.Services.AddHttpContextAccessor();
		builder.Services.AddScoped<ITenantProvider, TenantProvider>();
		builder.Services.AddDbContext<PerfumeTrackerContext>(o => o.UseNpgsql(connection, pg => pg.UseVector()));
		builder.Services.AddOptions<WeeklyDigestOptions>();
		builder.Services.AddOptions<OpenAIOptions>();
		builder.Services.AddScoped<IDigestCandidates, DigestCandidates>();
		builder.Services.AddSingleton<IDigestModel>(Model);
		builder.Services.AddScoped<WeeklyDigestAgent>();
		builder.Services.AddSingleton<WeeklyDigestBackgroundService>();
		builder.Services.AddAuthentication("test").AddScheme<AuthenticationSchemeOptions, DigestTestAuthentication>("test", _ => { });
		builder.Services.AddAuthorization(o => o.AddPolicy(Policies.READ, p => p.RequireAuthenticatedUser().RequireRole(Roles.USER)));
		App = builder.Build();
		App.UseAuthentication();
		App.UseAuthorization();
		new GetWeeklyDigests().AddRoutes(App);
		using (var scope = App.Services.CreateScope()) await scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>().Database.MigrateAsync(TestContext.Current.CancellationToken);
		await App.StartAsync(TestContext.Current.CancellationToken);
	}

	public async Task<Guid> NewUser(bool enabled = true, int perfumeCount = 6) {
		using var scope = App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var id = Guid.NewGuid();
		createdUsers.Add(id);
		db.TenantProvider = new FixedTenant(id);
		db.Users.Add(new PerfumeIdentityUser { Id = id, UserName = $"digest-{id}", WeeklyDigestEnabled = enabled });
		db.UserProfiles.Add(new UserProfile { Id = id });
		for (var i = 0; i < perfumeCount; i++) {
			var perfume = new Perfume { UserId = id, House = $"House {i}", PerfumeName = $"Perfume {i}", Family = "Woody", Parfumeur = "Tester",
				Ml = 50, MlLeft = 40, LatestRating = 8, LastWorn = DateTime.UtcNow.AddDays(-i - 10), WearCount = i };
			db.Perfumes.Add(perfume);
			db.PerfumeDocuments.Add(new PerfumeDocument { Id = perfume.Id, UserId = id, Embedding = new Pgvector.Vector(Enumerable.Repeat((float)i / 6, 1536).ToArray()) });
		}
		await db.SaveChangesAsync(TestContext.Current.CancellationToken);
		return id;
	}

	public async ValueTask DisposeAsync() {
		if (createdUsers.Count == 0) { if (App != null) await App.DisposeAsync(); return; }
		using (var scope = App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			await db.WeeklyDigests.IgnoreQueryFilters().Where(d => createdUsers.Contains(d.UserId)).ExecuteDeleteAsync();
			await db.PerfumeDocuments.IgnoreQueryFilters().Where(p => createdUsers.Contains(p.UserId)).ExecuteDeleteAsync();
			await db.Perfumes.IgnoreQueryFilters().Where(p => createdUsers.Contains(p.UserId)).ExecuteDeleteAsync();
			await db.UserProfiles.IgnoreQueryFilters().Where(p => createdUsers.Contains(p.Id)).ExecuteDeleteAsync();
			await db.Users.Where(u => createdUsers.Contains(u.Id)).ExecuteDeleteAsync();
		}
		await App.DisposeAsync();
	}
	public sealed class FixedTenant(Guid id) : ITenantProvider { public Guid? GetCurrentUserId() => id; }
}

public class DigestTestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
	: AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder) {
	protected override Task<AuthenticateResult> HandleAuthenticateAsync() {
		if (!Guid.TryParse(Request.Headers["X-Test-User"], out var user)) return Task.FromResult(AuthenticateResult.NoResult());
		var identity = new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, user.ToString()), new Claim(ClaimTypes.Role, Roles.USER)], Scheme.Name);
		return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
	}
}

public class DigestTestModel : IDigestModel {
	public int Requests;
	public TaskCompletionSource? Started { get; set; }
	public TaskCompletionSource? Release { get; set; }
	public bool Fail { get; set; }
	public async Task<JsonObject> Generate(JsonObject request, CancellationToken ct) {
		Interlocked.Increment(ref Requests);
		if (Fail) throw new HttpRequestException("Test transient failure");
		if (Started != null && Release != null) { Started.TrySetResult(); await Release.Task.WaitAsync(ct); }
		var final = request["tool_choice"]!.GetValue<string>() == "none";
		var text = "Evidence gathered.";
		if (final) {
			var facts = JsonNode.Parse(request["input"]![1]!["content"]!.GetValue<string>())!.AsObject();
			var categories = facts["candidatePools"]!.AsObject().Select(pool => new DigestCategory(Enum.Parse<RecommendationStrategy>(pool.Key), "This week’s inspiration.", "",
				pool.Value!.AsArray().Take(5).Select(p => new DigestPick(Guid.Parse(p!.GetValue<string>()), "Try these notes this week.")).ToList())).ToList();
			text = JsonSerializer.Serialize(new DigestPayload("Enjoy your collection.", "Autumn afternoon", categories), DigestJson.Options);
		}
		return new JsonObject { ["id"] = "resp_test", ["object"] = "response", ["created_at"] = 0, ["status"] = "completed", ["model"] = "test",
			["output"] = JsonSerializer.SerializeToNode(new[] { new { type = "message", id = "msg_test", role = "assistant", status = "completed",
				content = new[] { new { type = "output_text", text, annotations = Array.Empty<object>() } } } }),
			["usage"] = JsonNode.Parse("""{"input_tokens":900,"output_tokens":100,"total_tokens":1000}""") };
	}
}

[Collection("Weekly digest database")]
public class WeeklyDigestIntegrationTests(WeeklyDigestDatabase fixture) {
	private static CancellationToken Ct => TestContext.Current.CancellationToken;
	private static DateTime Week => WeeklyDigestSchedule.LatestDueWeek(DateTime.UtcNow);
	private WeeklyDigestBackgroundService Job => fixture.App.Services.GetRequiredService<WeeklyDigestBackgroundService>();

	[Fact]
	public async Task DisabledUser_DoesNotCreateRun() {
		var user = await fixture.NewUser(false);
		var before = fixture.Model.Requests;
		await Job.GenerateForUser(user, Week, Ct);
		using var scope = fixture.App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		Assert.False(await db.WeeklyDigests.IgnoreQueryFilters().AnyAsync(d => d.UserId == user, Ct));
		Assert.Equal(before, fixture.Model.Requests);
	}

	[Fact]
	public async Task CandidatePools_TranslateAllQueries_AndStayWithinTenant() {
		var user = await fixture.NewUser();
		using var scope = fixture.App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(user);
		var provider = scope.ServiceProvider.GetRequiredService<IDigestCandidates>();
		foreach (var category in Enum.GetValues<RecommendationStrategy>()) {
			var page = await provider.Get(category, 0, 3, null, Ct);
			var next = await provider.Get(category, 3, 3, null, Ct);
			Assert.Equal(6, page.Total);
			Assert.Equal(6, page.Perfumes.Concat(next.Perfumes).Select(p => p.Id).Distinct().Count());
		}
		Assert.Equal(6, (await provider.Get(RecommendationStrategy.MoodOrOccasion, 0, 30, "woody", Ct)).Perfumes.Count);
		var neverWorn = new Perfume { UserId = user, House = "House", PerfumeName = "Unworn", Family = "Woody", Parfumeur = "Tester", Ml = 50, MlLeft = 50, LatestRating = 8 };
		db.Perfumes.Add(neverWorn);
		await db.SaveChangesAsync(Ct);
		Assert.Equal(neverWorn.Id, (await provider.Get(RecommendationStrategy.LongestTimeAgo, 0, 1, null, Ct)).Perfumes[0].Id);
	}

	[Fact]
	public async Task EmptyCollection_IsSkippedWithoutModelUsage() {
		var user = await fixture.NewUser(perfumeCount: 0);
		var before = fixture.Model.Requests;
		await Job.GenerateForUser(user, Week, Ct);
		using var scope = fixture.App.Services.CreateScope();
		var run = await scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>().WeeklyDigests.IgnoreQueryFilters().SingleAsync(d => d.UserId == user, Ct);
		Assert.Equal("Skipped", run.Status);
		Assert.Equal(0, run.TokensUsed);
		Assert.Equal(before, fixture.Model.Requests);
	}

	[Fact]
	public async Task ActiveLease_PreventsClaimAndCost() {
		var user = await fixture.NewUser();
		using (var scope = fixture.App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			db.WeeklyDigests.Add(new WeeklyDigest { UserId = user, WeekStart = Week, Status = "Running", Model = "test", Attempts = 1, TokensUsed = 50_000,
				LeaseOwner = Guid.NewGuid(), LeaseUntil = DateTime.UtcNow.AddMinutes(5) });
			await db.SaveChangesAsync(Ct);
		}
		var before = fixture.Model.Requests;
		await Job.GenerateForUser(user, Week, Ct);
		Assert.Equal(before, fixture.Model.Requests);
	}

	[Fact]
	public async Task CancelledRequest_PreservesReservationAndCheckpointForRecovery() {
		var user = await fixture.NewUser();
		fixture.Model.Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Model.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		using var stopping = CancellationTokenSource.CreateLinkedTokenSource(Ct);
		try {
			var running = Job.GenerateForUser(user, Week, stopping.Token);
			await fixture.Model.Started.Task.WaitAsync(TimeSpan.FromSeconds(20), Ct);
			await stopping.CancelAsync();
			await Assert.ThrowsAnyAsync<OperationCanceledException>(() => running);
		} finally { fixture.Model.Release?.TrySetResult(); fixture.Model.Started = null; fixture.Model.Release = null; }
		using (var scope = fixture.App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			var run = await db.WeeklyDigests.IgnoreQueryFilters().SingleAsync(d => d.UserId == user, Ct);
			Assert.Equal("Running", run.Status);
			Assert.Equal(25_000, run.TokensUsed);
			Assert.Contains("explorationRequests", run.Checkpoint);
			await db.WeeklyDigests.IgnoreQueryFilters().Where(d => d.Id == run.Id).ExecuteUpdateAsync(s => s.SetProperty(d => d.LeaseUntil, (DateTime?)DateTime.UtcNow.AddMinutes(-1)), Ct);
		}
		await Job.GenerateForUser(user, Week, Ct);
		using var read = fixture.App.Services.CreateScope();
		var recovered = await read.ServiceProvider.GetRequiredService<PerfumeTrackerContext>().WeeklyDigests.IgnoreQueryFilters().SingleAsync(d => d.UserId == user, Ct);
		Assert.Equal("Completed", recovered.Status);
		Assert.Equal(27_000, recovered.TokensUsed);
	}

	[Fact]
	public async Task ConcurrentJobs_GenerateOnce_WithoutRecommendationSideEffects() {
		var user = await fixture.NewUser();
		fixture.Model.Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
		fixture.Model.Release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		try {
			var first = Job.GenerateForUser(user, Week, Ct);
			await fixture.Model.Started.Task.WaitAsync(TimeSpan.FromSeconds(20), Ct);
			await Job.GenerateForUser(user, Week, Ct);
			fixture.Model.Release.TrySetResult();
			await first;
			await Job.GenerateForUser(user, Week, Ct);
			using var scope = fixture.App.Services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(user);
			var run = await db.WeeklyDigests.SingleAsync(Ct);
			Assert.Equal("Completed", run.Status);
			Assert.Equal(1, run.Attempts);
			Assert.Equal(2000, run.TokensUsed);
			Assert.Null(run.LeaseOwner);
			Assert.False(await db.PerfumeRecommendations.AnyAsync(Ct));
			Assert.False(await db.OutboxMessages.AnyAsync(Ct));
		} finally { fixture.Model.Release?.TrySetResult(); fixture.Model.Started = null; fixture.Model.Release = null; }
	}

	[Fact]
	public async Task ExpiredLease_RecoversWithoutResettingBudget() {
		var user = await fixture.NewUser();
		using (var scope = fixture.App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			db.WeeklyDigests.Add(new WeeklyDigest { UserId = user, WeekStart = Week, Status = "Running", Model = "test", Attempts = 1, TokensUsed = 50_000,
				LeaseOwner = Guid.NewGuid(), LeaseUntil = DateTime.UtcNow.AddMinutes(-5) });
			await db.SaveChangesAsync(Ct);
		}
		await Job.GenerateForUser(user, Week, Ct);
		using var read = fixture.App.Services.CreateScope();
		var run = await read.ServiceProvider.GetRequiredService<PerfumeTrackerContext>().WeeklyDigests.IgnoreQueryFilters().SingleAsync(d => d.UserId == user, Ct);
		Assert.Equal("Completed", run.Status);
		Assert.Equal(2, run.Attempts);
		Assert.Equal(52_000, run.TokensUsed);
	}

	[Fact]
	public async Task TransientFailures_StopAtThreeAttempts_AndKeepReservations() {
		var user = await fixture.NewUser();
		fixture.Model.Fail = true;
		try { for (var attempt = 0; attempt < 4; attempt++) await Job.GenerateForUser(user, Week, Ct); }
		finally { fixture.Model.Fail = false; }
		using var scope = fixture.App.Services.CreateScope();
		var run = await scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>().WeeklyDigests.IgnoreQueryFilters().SingleAsync(d => d.UserId == user, Ct);
		Assert.Equal("Failed", run.Status);
		Assert.Equal(3, run.Attempts);
		Assert.Equal(75_000, run.TokensUsed);
	}

	[Fact]
	public async Task ReadApi_RequiresAuthentication_HidesOtherTenants_AndRetainsDeletedNames() {
		var user = await fixture.NewUser();
		await Job.GenerateForUser(user, Week, Ct);
		Guid digestId;
		Guid perfumeId;
		using (var scope = fixture.App.Services.CreateScope()) {
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(user);
			digestId = await db.WeeklyDigests.Select(d => d.Id).SingleAsync(Ct);
			perfumeId = await db.Perfumes.Select(p => p.Id).FirstAsync(Ct);
			await db.Perfumes.Where(p => p.Id == perfumeId).ExecuteUpdateAsync(s => s.SetProperty(p => p.IsDeleted, true), Ct);
		}
		using var client = fixture.App.GetTestClient();
		Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/weekly-digests", Ct)).StatusCode);
		client.DefaultRequestHeaders.Add("X-Test-User", Guid.NewGuid().ToString());
		Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/weekly-digests/{digestId}", Ct)).StatusCode);
		var otherHistory = JsonNode.Parse(await (await client.GetAsync("/api/weekly-digests", Ct)).Content.ReadAsStringAsync(Ct))!;
		Assert.Equal(0, otherHistory["total"]!.GetValue<int>());
		client.DefaultRequestHeaders.Remove("X-Test-User");
		client.DefaultRequestHeaders.Add("X-Test-User", user.ToString());
		var response = await client.GetAsync($"/api/weekly-digests/{digestId}", Ct);
		Assert.Equal(HttpStatusCode.OK, response.StatusCode);
		var detail = JsonNode.Parse(await response.Content.ReadAsStringAsync(Ct))!;
		var deleted = detail["digest"]!["categories"]!.AsArray().SelectMany(c => c!["picks"]!.AsArray()).First(p => p!["perfumeId"]!.GetValue<Guid>() == perfumeId)!;
		Assert.False(deleted["isAvailable"]!.GetValue<bool>());
		Assert.False(string.IsNullOrWhiteSpace(deleted["perfumeName"]!.GetValue<string>()));
		Assert.Equal(HttpStatusCode.BadRequest, (await client.GetAsync("/api/weekly-digests?page=0", Ct)).StatusCode);
	}
}
