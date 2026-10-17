#pragma warning disable OPENAI001
using System.ClientModel.Primitives;
using System.Text.Json;
using Moq;
using OpenAI.Responses;
using PerfumeTracker.Evals.Cases;
using PerfumeTracker.Evals.Harness;
using PerfumeTracker.Evals.Scoring;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.xTests.Tests;

public class EvalCollectionScorerTests {
	[Fact]
	public async Task Judge_ReceivesReturnedToolEvidence_WithBoundedSize() {
		string? request = null;
		var client = new Mock<IResponsesClient>();
		client.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.Callback<CreateResponseOptions, CancellationToken>((options, _) => request = ModelReaderWriter.Write(options).ToString())
			.ThrowsAsync(new InvalidOperationException("No network in this test"));
		var trajectory = new CaseTrajectory {
			CaseId = "test", UserMessage = "test", FinalAnswer = "answer",
			ToolCalls = [new("call", "filter_owned_perfumes", "{}", "returned-bottle-evidence" + new string('x', 13000) + "omitted-tail-marker")]
		};
		var context = new EvalScoringContext {
			OwnedPerfumeNames = Context.OwnedPerfumeNames, RegisteredToolNames = Context.RegisteredToolNames,
			MaxIterations = 10, MaxResultsPerToolCall = 50, MarketplaceAvailable = false,
			JudgeClient = client.Object, JudgeModel = "test-model"
		};
		await new LlmJudgeScorer().ScoreAsync(new EvalCase { Rubric = "Use returned evidence" }, trajectory, context, TestContext.Current.CancellationToken);
		Assert.NotNull(request);
		Assert.Contains("returned-bottle-evidence", request);
		Assert.DoesNotContain("omitted-tail-marker", request);
	}
	private static readonly Guid PerfumeId = Guid.Parse("99999999-1111-2222-3333-444444444444");
	private static EvalScoringContext Context => new() {
		OwnedPerfumeNames = new Dictionary<Guid, string> { [PerfumeId] = "Rebuy Lab - Full Sample" },
		RegisteredToolNames = ["filter_owned_perfumes"], MaxIterations = 10, MaxResultsPerToolCall = 50, MarketplaceAvailable = false
	};
	private static CaseTrajectory Trajectory(string arguments, string answer = "answer") => new() {
		CaseId = "test", UserMessage = "test", FinalAnswer = answer,
		ToolCalls = [new ToolCallTrace("call", "filter_owned_perfumes", arguments, "[]")]
	};

	[Theory]
	[InlineData("{\"remainingPercentBelow\":10.0,\"onlyAvailable\":false,\"count\":20}", ScoreOutcome.Pass)]
	[InlineData("{\"remainingPercentBelow\":20,\"onlyAvailable\":false}", ScoreOutcome.Fail)]
	[InlineData("{\"remainingPercentBelow\":10}", ScoreOutcome.Fail)]
	[InlineData("{\"remainingPercentBelow\":\"10\",\"onlyAvailable\":false}", ScoreOutcome.Fail)]
	[InlineData("not json", ScoreOutcome.Fail)]
	public async Task RequiredArguments_MatchSubsetWithNumericEquality(string arguments, ScoreOutcome outcome) {
		var evalCase = new EvalCase {
			RequiredToolArguments = [new ExpectedToolArguments {
				Tool = "filter_owned_perfumes",
				Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"remainingPercentBelow":10,"onlyAvailable":false}""")!
			}]
		};
		var score = await new ToolArgumentScorer().ScoreAsync(evalCase, Trajectory(arguments), Context, TestContext.Current.CancellationToken);
		Assert.Equal(outcome, score.Outcome);
	}

	[Fact]
	public async Task RequiredArguments_MustMatchOneWholeCall() {
		var evalCase = new EvalCase {
			RequiredToolArguments = [new ExpectedToolArguments {
				Tool = "filter_owned_perfumes",
				Arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>("""{"ratingAbove":3,"onlyAvailable":false}""")!
			}]
		};
		var trajectory = new CaseTrajectory {
			CaseId = "test", UserMessage = "test", FinalAnswer = "answer",
			ToolCalls = [new("a", "filter_owned_perfumes", """{"ratingAbove":3}""", "[]"),
				new("b", "filter_owned_perfumes", """{"onlyAvailable":false}""", "[]")]
		};
		Assert.Equal(ScoreOutcome.Fail, (await new ToolArgumentScorer().ScoreAsync(evalCase, trajectory, Context, TestContext.Current.CancellationToken)).Outcome);
	}

	[Fact]
	public async Task ForbiddenOwnedLink_FailsEvenWhenItIsOwned() {
		var score = await new PerfumeReferenceScorer().ScoreAsync(
			new EvalCase { ForbiddenOwnedLinks = ["Rebuy Lab - Full Sample"] },
			Trajectory("{}", $"[Full Sample](/perfumes/{PerfumeId})"), Context, TestContext.Current.CancellationToken);
		Assert.Equal(ScoreOutcome.Fail, score.Outcome);
	}

	[Fact]
	public async Task Dataset_HasValidReferencesAndFivePointRatings() {
		var cases = await EvalCaseLoader.LoadAsync(Path.Combine(AppContext.BaseDirectory, "EvalCases", "chat-agent.cases.json"), null, TestContext.Current.CancellationToken);
		Assert.Contains(cases, c => c.Id == "rebuy-favorites");
		Assert.All(cases.SelectMany(c => c.ExpectedOwnedLinks.Concat(c.ForbiddenOwnedLinks)),
			name => Assert.Contains(EvalWardrobe.Perfumes, p => p.DisplayName == name));
		Assert.All(EvalWardrobe.Perfumes, p => Assert.InRange(p.Rating, 0m, 5m));
	}
}
