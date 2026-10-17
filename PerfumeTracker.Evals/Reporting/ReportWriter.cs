using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PerfumeTracker.Evals.Scoring;

namespace PerfumeTracker.Evals.Reporting;

public static class ReportWriter {
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() }
	};

	public static async Task<string> WriteAsync(EvalReport report, string outputDirectory, string? baselinePath, CancellationToken cancellationToken) {
		Directory.CreateDirectory(outputDirectory);
		var resultsPath = Path.Combine(outputDirectory, "results.json");
		await File.WriteAllTextAsync(resultsPath, JsonSerializer.Serialize(report, JsonOptions), cancellationToken);

		var baselineLines = string.IsNullOrWhiteSpace(baselinePath)
			? []
			: BaselineComparison.Compare(baselinePath, report.Cases);

		var markdown = BuildMarkdown(report, resultsPath, baselineLines);
		await File.WriteAllTextAsync(Path.Combine(outputDirectory, "summary.md"), markdown, cancellationToken);
		return markdown;
	}

	private static string BuildMarkdown(EvalReport report, string resultsPath, IReadOnlyList<string> baselineLines) {
		var builder = new StringBuilder();
		builder.AppendLine("# Chat agent eval report");
		builder.AppendLine();
		builder.AppendLine($"- Mode: `{report.Mode}`");
		builder.AppendLine($"- Prompt fingerprint: `{report.PromptFingerprint}`");
		builder.AppendLine($"- Subject model: `{report.Model ?? "(not configured)"}`");
		builder.AppendLine($"- Judge model: `{report.JudgeModel ?? "(judge disabled)"}`");
		builder.AppendLine($"- Started: {report.StartedAt:u}");
		builder.AppendLine($"- Duration: {(report.DurationMs / 1000.0).ToString("0.0", CultureInfo.InvariantCulture)}s");
		builder.AppendLine($"- Passed: {report.Passed}, failed: {report.Failed}, errored: {report.Errored}, skipped: {report.Skipped}");
		builder.AppendLine($"- Tokens: {report.InputTokens} in / {report.OutputTokens} out");
		builder.AppendLine($"- Full results: `{resultsPath}`");
		builder.AppendLine();

		builder.AppendLine("| Case | Category | Status | Score | Notes |");
		builder.AppendLine("| --- | --- | --- | --- | --- |");
		foreach (var result in report.Cases) {
			var failed = result.Scores.Where(score => score.Outcome == ScoreOutcome.Fail).Select(score => score.Scorer).ToList();
			var notes = result.Note
				?? (failed.Count > 0 ? $"failed: {string.Join(", ", failed)}" : result.Trajectory?.Error);
			builder.AppendLine($"| {result.CaseId} | {result.Category} | {result.Status} | {Score(result).ToString("0.00", CultureInfo.InvariantCulture)} | {Escape(notes)} |");
		}
		builder.AppendLine();

		foreach (var result in report.Cases.Where(c => c.Status is CaseStatus.Failed or CaseStatus.Error)) {
			builder.AppendLine($"## {result.CaseId}");
			foreach (var score in result.Scores) {
				builder.AppendLine($"- **{score.Scorer}** ({score.Outcome}): {Escape(score.Detail)}");
			}
			if (result.Trajectory != null) {
				builder.AppendLine($"- Tools: {(result.Trajectory.CalledTools.Count == 0 ? "none" : string.Join(", ", result.Trajectory.CalledTools))}");
				builder.AppendLine($"- Answer: {Escape(Truncate(result.Trajectory.FinalAnswer, 600))}");
			}
			builder.AppendLine();
		}

		if (baselineLines.Count > 0) {
			builder.AppendLine("## Baseline comparison");
			foreach (var line in baselineLines) builder.AppendLine($"- {line}");
			builder.AppendLine();
		}

		return builder.ToString();
	}

	private static double Score(CaseResult result) {
		var scored = result.Scores.Where(score => score.Score.HasValue).Select(score => score.Score!.Value).ToList();
		return scored.Count == 0 ? (result.IsGood ? 1 : 0) : scored.Average();
	}

	private static string Escape(string? value) => (value ?? string.Empty).Replace("|", "\\|").ReplaceLineEndings(" ");

	private static string Truncate(string value, int max) => value.Length <= max ? value : value[..max] + "...";
}
