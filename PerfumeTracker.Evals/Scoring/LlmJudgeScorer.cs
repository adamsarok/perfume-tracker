using System.Text;
using System.Text.Json;
using OpenAI.Responses;
using PerfumeTracker.Evals.Cases;
using PerfumeTracker.Evals.Harness;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.Evals.Scoring;

/// <summary>
/// Rubric grading with a separate model call. The judge prompt and the response contract are
/// pinned here so a judge change is a reviewable diff, and skipping is explicit rather than silent.
/// </summary>
public sealed class LlmJudgeScorer : IEvalScorer {
	private static readonly JsonSerializerOptions VerdictOptions = new() { PropertyNameCaseInsensitive = true };

	public string Name => "llm-judge";

	public async Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken) {
		if (!evalCase.Judge) return ScoreResult.Skipped(Name, "case disables the judge");
		if (string.IsNullOrWhiteSpace(evalCase.Rubric)) return ScoreResult.Skipped(Name, "case has no rubric");
		if (context.JudgeClient == null || string.IsNullOrWhiteSpace(context.JudgeModel)) {
			return ScoreResult.Skipped(Name, "no judge model configured (pass --judge-model or --model)");
		}
		if (trajectory.Error != null) return ScoreResult.Skipped(Name, "case errored, nothing to judge");

		var options = ChatAgentResponseProtocol.CreateOptions(context.JudgeModel, [
			ResponseItem.CreateSystemMessageItem(
				"You grade answers from a perfume-collection chat assistant. Be strict but fair. " +
				"Reply with JSON only, no markdown: {\"pass\": true|false, \"score\": <0..1>, \"reasons\": \"<one or two sentences>\"}."),
			ResponseItem.CreateUserMessageItem(BuildJudgePrompt(evalCase, trajectory))
		], [], allowTools: false);
		options.MaxOutputTokenCount = 1024;

		ResponseResult response;
		try {
			response = await context.JudgeClient.CreateResponseAsync(options, cancellationToken);
		} catch (Exception ex) {
			return ScoreResult.Skipped(Name, $"judge call failed: {ex.Message}");
		}

		var text = response.GetOutputText()?.Trim() ?? string.Empty;
		var verdict = ParseVerdict(text);
		if (verdict == null) return ScoreResult.Fail(Name, $"judge returned unparseable verdict: '{Truncate(text)}'");

		var detail = $"score {verdict.Score.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}: {Truncate(verdict.Reasons)}";
		return verdict.Pass ? ScoreResult.Pass(Name, detail) : ScoreResult.Fail(Name, detail);
	}

	private static string BuildJudgePrompt(EvalCase evalCase, CaseTrajectory trajectory) {
		var builder = new StringBuilder();
		builder.AppendLine("## User message");
		builder.AppendLine(evalCase.Message);
		builder.AppendLine();
		builder.AppendLine("## Rubric");
		builder.AppendLine(evalCase.Rubric);
		builder.AppendLine();
		builder.AppendLine("## Tool calls the assistant made");
		if (trajectory.ToolCalls.Count == 0) {
			builder.AppendLine("(none)");
		} else {
			foreach (var call in trajectory.ToolCalls) {
				builder.AppendLine($"- {call.Name}({Truncate(call.Arguments, 400)})");
				builder.AppendLine($"  Returned evidence: {Truncate(call.Result ?? "(no result captured)", 12000)}");
			}
		}
		builder.AppendLine();
		builder.AppendLine("## Assistant answer");
		builder.AppendLine(trajectory.FinalAnswer);
		return builder.ToString();
	}

	private static JudgeVerdict? ParseVerdict(string text) {
		var json = StripCodeFence(text);
		try {
			var verdict = JsonSerializer.Deserialize<JudgeVerdict>(json, VerdictOptions);
			if (verdict != null) return verdict;
		} catch (JsonException) {
			// Fall through to the lenient path below.
		}

		var lower = text.ToLowerInvariant();
		if (lower.Contains("\"pass\": true") || lower.Contains("\"pass\":true")) return new JudgeVerdict { Pass = true, Score = 1, Reasons = text };
		if (lower.Contains("\"pass\": false") || lower.Contains("\"pass\":false")) return new JudgeVerdict { Pass = false, Score = 0, Reasons = text };
		return null;
	}

	private static string StripCodeFence(string text) {
		var trimmed = text.Trim();
		if (!trimmed.StartsWith("```", StringComparison.Ordinal)) return trimmed;
		var firstNewline = trimmed.IndexOf('\n');
		var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
		if (firstNewline < 0 || lastFence <= firstNewline) return trimmed;
		return trimmed[(firstNewline + 1)..lastFence].Trim();
	}

	private static string Truncate(string value, int max = 300) =>
		value.Length <= max ? value : value[..max] + "...";

	private sealed class JudgeVerdict {
		public bool Pass { get; set; }
		public double Score { get; set; }
		public string Reasons { get; set; } = string.Empty;
	}
}
