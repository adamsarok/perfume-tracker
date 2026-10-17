using PerfumeTracker.Evals.Cases;
using PerfumeTracker.Evals.Harness;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.Evals.Scoring;

public enum ScoreOutcome {
	Pass,
	Fail,
	Skipped
}

public sealed record ScoreResult(string Scorer, ScoreOutcome Outcome, double? Score, string Detail) {
	public static ScoreResult Pass(string scorer, string detail) => new(scorer, ScoreOutcome.Pass, 1, detail);
	public static ScoreResult Fail(string scorer, string detail) => new(scorer, ScoreOutcome.Fail, 0, detail);
	public static ScoreResult Skipped(string scorer, string detail) => new(scorer, ScoreOutcome.Skipped, null, detail);
}

public sealed class EvalScoringContext {
	public required IReadOnlyDictionary<Guid, string> OwnedPerfumeNames { get; init; }
	public required IReadOnlyList<string> RegisteredToolNames { get; init; }
	public required int MaxIterations { get; init; }
	public required int MaxResultsPerToolCall { get; init; }
	public required bool MarketplaceAvailable { get; init; }
	public IResponsesClient? JudgeClient { get; init; }
	public string? JudgeModel { get; init; }
}

public interface IEvalScorer {
	string Name { get; }
	Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken);
}
