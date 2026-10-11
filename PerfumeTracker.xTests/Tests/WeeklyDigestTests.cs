using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.xTests.Tests;

public class WeeklyDigestTests {
	[Theory]
	[InlineData("2026-10-12T05:59:59Z", "2026-10-05")]
	[InlineData("2026-10-12T06:00:00Z", "2026-10-12")]
	[InlineData("2026-10-18T23:59:59Z", "2026-10-12")]
	[InlineData("2027-01-01T12:00:00Z", "2026-12-28")]
	public void Schedule_SelectsLatestDueWeek(string now, string expected) {
		Assert.Equal(expected, WeeklyDigestSchedule.LatestDueWeek(DateTime.Parse(now).ToUniversalTime()).ToString("yyyy-MM-dd"));
	}

	[Fact]
	public void Flag_IsDisabledAndNotSerialized() {
		var user = new PerfumeIdentityUser();
		Assert.False(user.WeeklyDigestEnabled);
		user.WeeklyDigestEnabled = true;
		Assert.DoesNotContain("WeeklyDigestEnabled", JsonSerializer.Serialize(user));
	}

	[Fact]
	public void Budget_ReservesFinalInputAndOutput() {
		var config = new WeeklyDigestOptions();
		Assert.True(config.IsValid());
		Assert.True(WeeklyDigestAgent.CanExplore(80_000, 5000, config));
		Assert.False(WeeklyDigestAgent.CanExplore(80_001, 5000, config));
		Assert.False(new WeeklyDigestOptions { FinalizationReserve = 10_000 }.IsValid());
	}

	[Fact]
	public async Task Agent_PreservesReasoningAndToolCallIds_ThenProducesValidatedSnapshot() {
		var candidates = new FakeCandidates();
		var model = new FakeModel(candidates);
		var run = NewRun();
		var savedReservations = new List<long>();
		var result = await Agent(candidates, model).Run(run, _ => { savedReservations.Add(run.TokensUsed); return Task.CompletedTask; }, TestContext.Current.CancellationToken);
		Assert.Equal(7, result.Categories.Count);
		Assert.All(result.Categories, c => {
			Assert.Equal(5, c.Picks.Count);
			Assert.All(c.Picks, p => Assert.Equal("House", p.House));
		});
		Assert.Equal(3000, run.TokensUsed);
		Assert.Contains(26_000, savedReservations);
		Assert.Equal(3, model.Requests.Count);
		var replay = model.Requests[1]["input"]!.AsArray();
		Assert.Contains(replay, i => i?["type"]?.GetValue<string>() == "reasoning" && i["encrypted_content"]!.GetValue<string>() == "encrypted-data");
		Assert.Contains(replay, i => i?["type"]?.GetValue<string>() == "function_call_output" && i["call_id"]!.GetValue<string>() == "call_one");
		Assert.Equal("none", model.Requests[^1]["tool_choice"]!.GetValue<string>());
		Assert.Equal("json_schema", model.Requests[^1]["text"]!["format"]!["type"]!.GetValue<string>());
	}

	[Fact]
	public async Task Agent_BudgetCutoffMakesOnlyFinalRequest() {
		var candidates = new FakeCandidates();
		var model = new FakeModel(candidates);
		var run = NewRun();
		run.TokensUsed = 109_000;
		await Agent(candidates, model).Run(run, _ => Task.CompletedTask, TestContext.Current.CancellationToken);
		Assert.Equal("none", Assert.Single(model.Requests)["tool_choice"]!.GetValue<string>());
		Assert.Equal(110_000, run.TokensUsed);
	}

	[Fact]
	public async Task Agent_ResumesPendingToolBeforeAnotherGeneration() {
		var candidates = new FakeCandidates();
		var checkpoint = new DigestCheckpoint { Context = JsonSerializer.SerializeToElement(new { season = "Autumn" }) };
		checkpoint.History.Add(JsonSerializer.SerializeToElement(new { type = "function_call", name = "get_digest_candidates", call_id = "pending_call",
			arguments = "{\"category\":\"Seasonal\",\"offset\":0,\"count\":30,\"search\":null}" }));
		foreach (var category in Enum.GetValues<RecommendationStrategy>()) {
			checkpoint.CategoryEvidence[category] = candidates.Perfumes.Select(p => p.Id).ToList();
		}
		checkpoint.Evidence = candidates.Perfumes.ToDictionary(p => p.Id);
		checkpoint.TotalEligible = 6;
		var run = NewRun();
		run.Checkpoint = JsonSerializer.Serialize(checkpoint, DigestJson.Options);
		var model = new FakeModel(candidates) { ExploreTools = false };
		await Agent(candidates, model).Run(run, _ => Task.CompletedTask, TestContext.Current.CancellationToken);
		Assert.Contains(model.Requests[0]["input"]!.AsArray(), i => i?["type"]?.GetValue<string>() == "function_call_output" && i["call_id"]!.GetValue<string>() == "pending_call");
	}

	[Fact]
	public async Task Agent_AmbiguousNetworkFailureKeepsWorstCaseReservation() {
		var candidates = new FakeCandidates();
		var run = NewRun();
		var model = new FakeModel(candidates) { ThrowOnGenerate = true };
		await Assert.ThrowsAsync<HttpRequestException>(() => Agent(candidates, model).Run(run, _ => Task.CompletedTask, TestContext.Current.CancellationToken));
		Assert.Equal(26_000, run.TokensUsed);
		var restored = JsonSerializer.Deserialize<DigestCheckpoint>(run.Checkpoint, DigestJson.Options)!;
		Assert.Equal(1, restored.ExplorationRequests);
	}

	[Theory]
	[InlineData("unknown")]
	[InlineData("duplicate")]
	[InlineData("missingCategory")]
	public void Validation_RejectsInvalidSelections(string problem) {
		var candidates = new FakeCandidates();
		var payload = candidates.Payload();
		if (problem == "unknown") payload.Categories[0].Picks[0] = new(Guid.NewGuid(), "Reason");
		if (problem == "duplicate") payload.Categories[0].Picks[1] = payload.Categories[0].Picks[0];
		if (problem == "missingCategory") payload.Categories.RemoveAt(0);
		Assert.Throws<InvalidDataException>(() => DigestValidation.Validate(payload, candidates.Perfumes.ToDictionary(p => p.Id), 6));
	}

	[Fact]
	public void Validation_AllowsExplainedSmallCollections() {
		var candidates = new FakeCandidates(2);
		DigestValidation.Validate(candidates.Payload(), candidates.Perfumes.ToDictionary(p => p.Id), 2);
		var payload = candidates.Payload();
		payload.Categories[0] = payload.Categories[0] with { ShortageReason = "" };
		Assert.Throws<InvalidDataException>(() => DigestValidation.Validate(payload, candidates.Perfumes.ToDictionary(p => p.Id), 2));
	}

	[Fact]
	public async Task Agent_RepairsMalformedFinalAnswerOnlyOnce() {
		var candidates = new FakeCandidates();
		var model = new FakeModel(candidates) { InvalidFinals = 1 };
		var run = NewRun();
		run.TokensUsed = 109_000;
		await Agent(candidates, model).Run(run, _ => Task.CompletedTask, TestContext.Current.CancellationToken);
		Assert.Equal(2, model.Requests.Count);
		Assert.True(JsonSerializer.Deserialize<DigestCheckpoint>(run.Checkpoint, DigestJson.Options)!.RepairUsed);
	}

	[Fact]
	public async Task Agent_RejectsTruncatedResponse() {
		var candidates = new FakeCandidates();
		var model = new FakeModel(candidates) { Incomplete = true };
		await Assert.ThrowsAsync<InvalidDataException>(() => Agent(candidates, model).Run(NewRun(), _ => Task.CompletedTask, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task Transport_CountsSchemaAndTools_AndNeverAutomaticallyRetries() {
		using var handler = new CaptureHandler();
		using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
		var model = new DigestModel(http, Options.Create(new OpenAIOptions { ApiKey = "test-key" }));
		var request = JsonNode.Parse("""{"model":"test","input":[],"tools":[],"text":{"format":{"type":"text"}},"max_output_tokens":25000,"store":false}""")!.AsObject();
		Assert.Equal(1000, await model.CountInput(request, TestContext.Current.CancellationToken));
		Assert.NotNull(handler.Requests[0]["text"]);
		Assert.Null(handler.Requests[0]["max_output_tokens"]);
		await Assert.ThrowsAsync<DigestApiException>(() => model.Generate(request, TestContext.Current.CancellationToken));
		Assert.Equal(2, handler.Requests.Count);
	}

	private static WeeklyDigest NewRun() => new() { UserId = Guid.NewGuid(), WeekStart = WeeklyDigestSchedule.LatestDueWeek(DateTime.UtcNow), Model = "test" };
	private static WeeklyDigestAgent Agent(IDigestCandidates candidates, IDigestModel model) =>
		new(candidates, model, Options.Create(new WeeklyDigestOptions()), NullLogger<WeeklyDigestAgent>.Instance);

	private sealed class FakeCandidates(int count = 6) : IDigestCandidates {
		public List<DigestCandidate> Perfumes { get; } = Enumerable.Range(0, count).Select(i => new DigestCandidate(Guid.NewGuid(), "House", $"Perfume {i}", "Woody", 8, 10, i, null, ["wood"], null)).ToList();
		public Task<object> GetContext(DateTime weekStart, CancellationToken ct) => Task.FromResult<object>(new { weekStart, season = "Autumn" });
		public Task<DigestCandidatePage> Get(RecommendationStrategy category, int offset, int count, string? search, CancellationToken ct) => Task.FromResult(new DigestCandidatePage(Perfumes.Count, offset, Perfumes.Skip(offset).Take(count).ToList()));
		public DigestPayload Payload() => new("Enjoy your collection.", "Quiet autumn afternoon", Enum.GetValues<RecommendationStrategy>()
			.Select(s => new DigestCategory(s, "A varied selection.", Perfumes.Count < 5 ? "Small collection." : "", Perfumes.Take(5).Select(p => new DigestPick(p.Id, "Try this woody scent.")).ToList())).ToList());
	}

	private sealed class FakeModel(FakeCandidates candidates) : IDigestModel {
		public List<JsonObject> Requests { get; } = [];
		public bool ExploreTools { get; set; } = true;
		public bool ThrowOnGenerate { get; set; }
		public bool Incomplete { get; set; }
		public int InvalidFinals { get; set; }
		public Task<int> CountInput(JsonObject request, CancellationToken ct) => Task.FromResult(1000);
		public Task<JsonObject> Generate(JsonObject request, CancellationToken ct) {
			Requests.Add((JsonObject)request.DeepClone());
			if (ThrowOnGenerate) throw new HttpRequestException("Network interrupted");
			var final = request["tool_choice"]!.GetValue<string>() == "none";
			var output = !final && ExploreTools && Requests.Count == 1
				? JsonNode.Parse("""[{"type":"reasoning","id":"rs_one","summary":[],"encrypted_content":"encrypted-data"},{"type":"function_call","id":"fc_one","call_id":"call_one","name":"get_digest_candidates","arguments":"{\"category\":\"Seasonal\",\"offset\":0,\"count\":30,\"search\":null}","status":"completed"}]""")!
				: JsonSerializer.SerializeToNode(new[] { new { type = "message", id = "msg_one", role = "assistant", status = "completed", content = new[] {
					new { type = "output_text", text = final ? (InvalidFinals-- > 0 ? "invalid JSON" : JsonSerializer.Serialize(candidates.Payload(), DigestJson.Options)) : "Enough evidence gathered.", annotations = Array.Empty<object>() }
				} } })!;
			return Task.FromResult(new JsonObject {
				["id"] = "resp_test", ["object"] = "response", ["created_at"] = 0, ["status"] = Incomplete ? "incomplete" : "completed", ["model"] = "test", ["output"] = output,
				["usage"] = JsonNode.Parse("""{"input_tokens":900,"output_tokens":100,"total_tokens":1000,"input_tokens_details":{"cached_tokens":0},"output_tokens_details":{"reasoning_tokens":20}}""")
			});
		}
	}

	private sealed class CaptureHandler : HttpMessageHandler {
		public List<JsonObject> Requests { get; } = [];
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
			Requests.Add(JsonNode.Parse(await request.Content!.ReadAsStringAsync(ct))!.AsObject());
			Assert.Equal("test-key", request.Headers.Authorization!.Parameter);
			return new HttpResponseMessage(request.RequestUri!.AbsolutePath.EndsWith("input_tokens", StringComparison.Ordinal) ? HttpStatusCode.OK : HttpStatusCode.TooManyRequests) {
				Content = new StringContent("{\"input_tokens\":1000}", Encoding.UTF8, "application/json")
			};
		}
	}
}
