using System.Text.Json;
using System.Text.Json.Nodes;
using PerfumeTracker.Evals;
using PerfumeTracker.Evals.Scoring;
using PerfumeTracker.Evals.WeeklyDigests;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.xTests.Tests;

public class WeeklyDigestEvalTests {
	[Fact]
	public async Task Cases_DefaultToNineSmokeCases_ProductionBudgetIsExplicit() {
		var path = Path.Combine(AppContext.BaseDirectory, "EvalCases", "weekly-digest.cases.json");
		var smoke = await DigestEvalCase.LoadAsync(path, null, TestContext.Current.CancellationToken);
		Assert.Equal(9, smoke.Count);
		Assert.All(smoke, c => { Assert.False(c.Extended); Assert.True(c.Budget.IsValid()); Assert.Equal(3, c.Budget.MaxExplorationRequests); });
		var production = Assert.Single(await DigestEvalCase.LoadAsync(path, "production-budget", TestContext.Current.CancellationToken));
		Assert.Equal(50, production.Budget.MaxExplorationRequests);
		Assert.Equal(150_000, production.Budget.MaxTokens);
		Assert.DoesNotContain(await DigestEvalCase.LoadAsync(path, "diverse", TestContext.Current.CancellationToken), c => c.Extended);
		Assert.Equal("chat", EvalRunOptions.Parse([]).Suite);
		Assert.Equal("weekly-digest", EvalRunOptions.Parse(["--suite", "weekly-digest"]).Suite);
		Assert.Throws<ArgumentException>(() => EvalRunOptions.Parse(["--suite", "unknown"]));
	}

	[Fact]
	public void Fixtures_AreStableAndUseFivePointRatings() {
		var evalCase = Case();
		Assert.Equal(JsonSerializer.Serialize(DigestEvalFixtures.Build(evalCase)), JsonSerializer.Serialize(DigestEvalFixtures.Build(evalCase)));
		Assert.All(DigestEvalFixtures.Build(evalCase).Perfumes, p => Assert.InRange(p.Rating, 1m, 5m));
		Assert.NotEqual(DigestEvalFixtures.Build(evalCase).UserId, DigestEvalFixtures.Build(new() { Id = "other" }).UserId);
		DigestEvalHost.ValidateConnection("Host=localhost;Database=digest_test");
		Assert.Throws<InvalidOperationException>(() => DigestEvalHost.ValidateConnection("Host=localhost;Database=production"));
	}

	[Fact]
	public async Task Recording_RoundTripsAndVerifiesEveryRequest() {
		var root = Path.Combine(Path.GetTempPath(), "digest-eval-tests", Guid.NewGuid().ToString());
		var ct = TestContext.Current.CancellationToken;
		try {
			var source = new StubDigestModel();
			var recorded = new DigestEvalTrajectory();
			var writer = new RecordedDigestModel(EvalMode.Record, root, "../unsafe-case", "fixture-v1", source, recorded);
			var first = JsonNode.Parse("""{"model":"test","input":[{"role":"user","content":"first"}]}""")!.AsObject();
			var second = JsonNode.Parse("""{"model":"test","tool_choice":"none","input":[]}""")!.AsObject();
			await writer.Generate(first, ct);
			await writer.Generate(second, ct);
			var replayed = new DigestEvalTrajectory();
			var reader = new RecordedDigestModel(EvalMode.Replay, root, "../unsafe-case", "fixture-v1", null, replayed);
			Assert.True(JsonNode.DeepEquals(recorded.ModelCalls[0].Response.Deserialize<JsonObject>(), await reader.Generate(first, ct)));
			await reader.Generate(second, ct);
			Assert.Equal(2, source.Calls);
			Assert.Equal(recorded.Fingerprint, replayed.Fingerprint);
			var mismatch = new RecordedDigestModel(EvalMode.Replay, root, "../unsafe-case", "fixture-v1", null, new());
			await mismatch.Generate(first, ct);
			var modified = (JsonObject)second.DeepClone();
			modified["input"] = new JsonArray("changed");
			var error = await Assert.ThrowsAsync<InvalidDataException>(() => mismatch.Generate(modified, ct));
			Assert.Contains("request mismatch", error.Message);
			var staleFixture = new RecordedDigestModel(EvalMode.Replay, root, "../unsafe-case", "fixture-v2", null, new());
			Assert.Contains("Missing digest recording", (await Assert.ThrowsAsync<InvalidDataException>(() => staleFixture.Generate(first, ct))).Message);
			Assert.Equal(RecordedDigestModel.Hash(first), RecordedDigestModel.Hash(JsonNode.Parse("""{"input":[{"content":"first","role":"user"}],"model":"test"}""")!));
		} finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
	}

	[Fact]
	public void Scoring_RejectsInventedAndRepeatedPicks_AndMissingShortageReasons() {
		var evalCase = Case("tiny");
		var valid = Trajectory(evalCase);
		Assert.All(DigestEvalScoring.Score(evalCase, valid), s => Assert.Equal(ScoreOutcome.Pass, s.Outcome));
		valid.Payload!.Categories[0].Picks[0] = new(Guid.NewGuid(), "Invented");
		Assert.Contains(DigestEvalScoring.Score(evalCase, valid), s => s.Scorer == "eligibility-grounding" && s.Outcome == ScoreOutcome.Fail);
		var repeated = Trajectory(evalCase);
		repeated.Payload!.Categories[0].Picks[1] = repeated.Payload.Categories[0].Picks[0];
		Assert.Contains(DigestEvalScoring.Score(evalCase, repeated), s => s.Scorer == "digest-validation" && s.Outcome == ScoreOutcome.Fail);
		var shortage = Trajectory(evalCase);
		shortage.Payload!.Categories[0] = shortage.Payload.Categories[0] with { ShortageReason = "" };
		Assert.Contains(DigestEvalScoring.Score(evalCase, shortage), s => s.Scorer == "digest-validation" && s.Outcome == ScoreOutcome.Fail);
	}

	[Fact]
	public async Task Recording_ReplaysTransientFailuresBeforeTheSuccessfulRetry() {
		var root = Path.Combine(Path.GetTempPath(), "digest-eval-tests", Guid.NewGuid().ToString());
		var ct = TestContext.Current.CancellationToken;
		try {
			var request = JsonNode.Parse("""{"model":"test","input":[],"max_output_tokens":10000}""")!.AsObject();
			var recorded = new DigestEvalTrajectory();
			var writer = new RecordedDigestModel(EvalMode.Record, root, "retry-test", "fixture-v1", new StubDigestModel { FailFirst = true }, recorded);
			var original = await Assert.ThrowsAsync<DigestApiException>(() => writer.Generate(request, ct));
			Assert.Equal(TimeSpan.FromMinutes(2), original.RetryAfter);
			await writer.Generate(request, ct);
			var replayed = new DigestEvalTrajectory();
			var reader = new RecordedDigestModel(EvalMode.Replay, root, "retry-test", "fixture-v1", null, replayed);
			var failure = await Assert.ThrowsAsync<DigestApiException>(() => reader.Generate(request, ct));
			Assert.Equal(520, failure.StatusCode);
			Assert.Equal(TimeSpan.Zero, failure.RetryAfter); // Replay skips wall-clock waits only.
			await reader.Generate(request, ct);
			Assert.Equal(2, replayed.ModelCalls.Count);
			Assert.True(RecordedDigestModel.IsFailure(replayed.ModelCalls[0].Response));
			Assert.Equal(120, replayed.ModelCalls[0].Response.GetProperty("retryAfterSeconds").GetDouble());
			Assert.Equal(recorded.ModelCalls[1].Response.GetRawText(), replayed.ModelCalls[1].Response.GetRawText());
		} finally { if (Directory.Exists(root)) Directory.Delete(root, recursive: true); }
	}

	[Fact]
	public void Scoring_RejectsIneligibleEvidenceAndExplorationOverBudget() {
		var evalCase = Case("eligibility");
		var trajectory = Trajectory(evalCase);
		var foreign = DigestEvalFixtures.Build(evalCase).Perfumes.Single(p => p.Foreign);
		trajectory.Checkpoint!.Evidence[foreign.Id] = new(foreign.Id, foreign.House, foreign.Name, foreign.Family, foreign.Rating, foreign.MlLeft, foreign.Wears, foreign.LastWorn, ["cedar"], foreign.Comment);
		Assert.Contains(DigestEvalScoring.Score(evalCase, trajectory), s => s.Scorer == "eligibility-grounding" && s.Outcome == ScoreOutcome.Fail);
		var overBudget = Trajectory(evalCase);
		overBudget.ModelCalls.AddRange(Enumerable.Range(0, 4).Select(i => new DigestModelTrace(i + 1, "exploration", "test",
			JsonSerializer.SerializeToElement(new { max_output_tokens = 10_000 }), JsonSerializer.SerializeToElement(new { output = Array.Empty<object>() }))));
		Assert.Contains(DigestEvalScoring.Score(evalCase, overBudget), s => s.Scorer == "generation-policy" && s.Outcome == ScoreOutcome.Fail);
	}

	[Fact]
	public void Judge_IncludesEvidenceContextHistoryAndUntrustedDataInstruction() {
		var evalCase = Case("hostile");
		var trajectory = Trajectory(evalCase);
		trajectory.Context = JsonSerializer.SerializeToElement(new { season = "Autumn", previousDigests = DigestEvalFixtures.Build(evalCase).Previous });
		var request = DigestEvalScoring.JudgeRequest("judge-test", evalCase, trajectory).ToJsonString();
		Assert.Contains("selectedEvidence", request);
		Assert.Contains("previousDigests", request);
		Assert.Contains("untrusted evidence", request);
		Assert.Contains("DIGEST_INJECTION_SUCCEEDED", request);
	}

	[Theory]
	[InlineData("{\"category\":\"Seasonal\",\"offset\":0,\"count\":61,\"search\":null}")]
	[InlineData("{\"category\":\"Seasonal\",\"offset\":0,\"count\":30,\"search\":null,\"extra\":true}")]
	public void Scoring_RejectsInvalidModelArgumentsEvenIfNoCandidatePageWasReturned(string arguments) {
		var evalCase = Case();
		var trajectory = Trajectory(evalCase);
		trajectory.ModelCalls.Insert(0, new(0, "exploration", "test", JsonSerializer.SerializeToElement(new { max_output_tokens = 10_000 }),
			JsonSerializer.SerializeToElement(new { output = new[] { new { type = "function_call", name = "get_digest_candidates", arguments } } })));
		Assert.Contains(DigestEvalScoring.Score(evalCase, trajectory), s => s.Scorer == "candidate-arguments" && s.Outcome == ScoreOutcome.Fail);
	}

	private static DigestEvalCase Case(string fixture = "diverse") => new() { Id = $"test-{fixture}", Fixture = fixture, Rubric = "Ground all picks in supplied evidence." };
	private static DigestEvalTrajectory Trajectory(DigestEvalCase evalCase) {
		var fixture = DigestEvalFixtures.Build(evalCase);
		var candidates = fixture.Perfumes.Where(p => !p.Foreign && !p.Deleted && p.MlLeft > 0 && p.Rating >= 3)
			.Select(p => new DigestCandidate(p.Id, p.House, p.Name, p.Family, p.Rating, p.MlLeft, p.Wears, p.LastWorn, p.Tags.ToList(), p.Comment)).ToList();
		return new() {
			Payload = new("A weekly selection.", "Autumn afternoon", Enum.GetValues<RecommendationStrategy>().Select(s => new DigestCategory(s, "Try these.",
				candidates.Count < 5 ? "Small collection." : "", candidates.Take(5).Select(p => new DigestPick(p.Id, "Supported reason.")).ToList())).ToList()),
			Checkpoint = new DigestCheckpoint { Evidence = candidates.ToDictionary(p => p.Id), TotalEligible = candidates.Count },
			CandidateCalls = [new("Seasonal", 0, 60, null, new(candidates.Count, 0, candidates))],
			ModelCalls = [new(0, "finalization", "test", JsonSerializer.SerializeToElement(new { max_output_tokens = 10_000 }),
				JsonSerializer.SerializeToElement(new { output = Array.Empty<object>() }))]
		};
	}
	private sealed class StubDigestModel : IDigestModel {
		public int Calls { get; private set; }
		public bool FailFirst { get; init; }
		public Task<JsonObject> Generate(JsonObject request, CancellationToken ct) {
			if (++Calls == 1 && FailFirst) throw new DigestApiException(520, TimeSpan.FromMinutes(2));
			return Task.FromResult(JsonNode.Parse("""{"status":"completed","output":[],"usage":{"input_tokens":5,"output_tokens":3,"total_tokens":8}}""")!.AsObject());
		}
	}
}
