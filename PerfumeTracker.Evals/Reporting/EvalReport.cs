using PerfumeTracker.Evals.Harness;
using PerfumeTracker.Evals.Scoring;

namespace PerfumeTracker.Evals.Reporting;

public enum CaseStatus {
	Passed,
	Failed,
	Skipped,
	Error
}

public sealed class CaseResult {
	public required string CaseId { get; init; }
	public required string Category { get; init; }
	public required CaseStatus Status { get; init; }
	public List<ScoreResult> Scores { get; init; } = [];
	public CaseTrajectory? Trajectory { get; init; }
	public PerfumeTracker.Evals.WeeklyDigests.DigestEvalTrajectory? DigestTrajectory { get; init; }
	public PerfumeTracker.Evals.WeeklyDigests.DigestQualityMetrics? DigestMetrics { get; init; }
	public string? Note { get; init; }

	public bool IsGood => Status == CaseStatus.Passed || Status == CaseStatus.Skipped;
}

public sealed class EvalReport {
	public string Suite { get; init; } = "chat";
	public string Mode { get; init; } = string.Empty;
	public string PromptFingerprint { get; init; } = string.Empty;
	public string? Model { get; init; }
	public string? JudgeModel { get; init; }
	public DateTime StartedAt { get; init; }
	public long DurationMs { get; init; }
	public int Passed { get; init; }
	public int Failed { get; init; }
	public int Skipped { get; init; }
	public int Errored { get; init; }
	public int InputTokens { get; init; }
	public int OutputTokens { get; init; }
	public List<CaseResult> Cases { get; init; } = [];
}
