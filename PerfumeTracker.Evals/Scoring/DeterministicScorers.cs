using System.Text.Json;
using System.Text.RegularExpressions;
using PerfumeTracker.Evals.Cases;
using PerfumeTracker.Evals.Harness;

namespace PerfumeTracker.Evals.Scoring;

/// <summary>Did the run finish and produce an answer at all?</summary>
public sealed class CompletionScorer : IEvalScorer {
	public string Name => "completion";

	public Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken) {
		if (trajectory.Error != null) return Task.FromResult(ScoreResult.Fail(Name, $"run failed: {trajectory.Error}"));
		if (string.IsNullOrWhiteSpace(trajectory.FinalAnswer)) return Task.FromResult(ScoreResult.Fail(Name, "no final answer was produced"));
		return Task.FromResult(ScoreResult.Pass(Name, $"answered after {trajectory.ChatModelCalls} chat model call(s)"));
	}
}

/// <summary>Deterministic tool-selection assertions: expected tools used, forbidden tools avoided.</summary>
public sealed class ToolSelectionScorer : IEvalScorer {
	public string Name => "tool-selection";

	public Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken) {
		var called = trajectory.CalledTools;
		var calledText = called.Count == 0 ? "none" : string.Join(", ", called);

		var missing = evalCase.ExpectedTools.Where(tool => !called.Contains(tool, StringComparer.Ordinal)).ToList();
		if (missing.Count > 0) {
			return Task.FromResult(ScoreResult.Fail(Name, $"expected tool(s) {string.Join(", ", missing)} were not called (called: {calledText})"));
		}

		var forbidden = evalCase.ForbiddenTools.Where(tool => called.Contains(tool, StringComparer.Ordinal)).ToList();
		if (forbidden.Count > 0) {
			return Task.FromResult(ScoreResult.Fail(Name, $"forbidden tool(s) {string.Join(", ", forbidden)} were called (called: {calledText})"));
		}

		if (evalCase.MaxToolCalls.HasValue && trajectory.ChatModelCalls > evalCase.MaxToolCalls.Value) {
			return Task.FromResult(ScoreResult.Fail(Name, $"used {trajectory.ChatModelCalls} model calls, case allows at most {evalCase.MaxToolCalls.Value}"));
		}

		return Task.FromResult(ScoreResult.Pass(Name, $"called: {calledText}"));
	}
}

/// <summary>Schema-level checks on the tool arguments the model chose.</summary>
public sealed class ToolArgumentScorer : IEvalScorer {
	public string Name => "tool-arguments";

	public Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken) {
		foreach (var call in trajectory.ToolCalls) {
			if (!context.RegisteredToolNames.Contains(call.Name, StringComparer.Ordinal)) {
				return Task.FromResult(ScoreResult.Fail(Name, $"called unregistered tool '{call.Name}'"));
			}

			JsonDocument document;
			try {
				document = JsonDocument.Parse(string.IsNullOrWhiteSpace(call.Arguments) ? "{}" : call.Arguments);
			} catch (JsonException ex) {
				return Task.FromResult(ScoreResult.Fail(Name, $"tool '{call.Name}' was called with unparseable arguments: {ex.Message}"));
			}

			using (document) {
				var root = document.RootElement;
				if (root.ValueKind != JsonValueKind.Object) {
					return Task.FromResult(ScoreResult.Fail(Name, $"tool '{call.Name}' arguments are not a JSON object"));
				}

				if (root.TryGetProperty("count", out var count)
					&& count.ValueKind == JsonValueKind.Number
					&& (count.GetInt32() < 1 || count.GetInt32() > context.MaxResultsPerToolCall)) {
					return Task.FromResult(ScoreResult.Fail(Name, $"tool '{call.Name}' asked for count={count.GetInt32()}, allowed range is 1..{context.MaxResultsPerToolCall}"));
				}

				if (root.TryGetProperty("perfumes", out var perfumes) && perfumes.ValueKind == JsonValueKind.Array
					&& (perfumes.GetArrayLength() < 1 || perfumes.GetArrayLength() > context.MaxResultsPerToolCall)) {
					return Task.FromResult(ScoreResult.Fail(Name, $"tool '{call.Name}' sent {perfumes.GetArrayLength()} perfumes, allowed range is 1..{context.MaxResultsPerToolCall}"));
				}
			}
		}

		return Task.FromResult(ScoreResult.Pass(Name, $"{trajectory.ToolCalls.Count} tool call(s) used valid arguments"));
	}
}

/// <summary>
/// Owned-perfume reference rules: every /perfumes/&lt;id&gt; link must point at a perfume the user
/// actually owns, and required perfumes must be linked.
/// </summary>
public sealed class PerfumeReferenceScorer : IEvalScorer {
	private static readonly Regex PerfumeLinkRegex = new(
		@"/perfumes/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
		RegexOptions.Compiled);

	public string Name => "perfume-references";

	public Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken) {
		var linkedIds = PerfumeLinkRegex.Matches(trajectory.FinalAnswer)
			.Select(match => Guid.TryParse(match.Groups["id"].Value, out var id) ? id : Guid.Empty)
			.Where(id => id != Guid.Empty)
			.Distinct()
			.ToList();

		var notOwned = linkedIds.Where(id => !context.OwnedPerfumeNames.ContainsKey(id)).ToList();
		if (notOwned.Count > 0) {
			return Task.FromResult(ScoreResult.Fail(Name,
				$"the answer links {notOwned.Count} perfume id(s) the user does not own: {string.Join(", ", notOwned.Select(id => id.ToString()))}"));
		}

		var linkedNames = linkedIds.Select(id => context.OwnedPerfumeNames[id]).ToList();
		var missing = evalCase.ExpectedOwnedLinks
			.Where(expected => !linkedNames.Contains(expected, StringComparer.OrdinalIgnoreCase))
			.ToList();
		if (missing.Count > 0) {
			return Task.FromResult(ScoreResult.Fail(Name,
				$"expected owned link(s) missing: {string.Join(", ", missing)} (linked: {(linkedNames.Count == 0 ? "none" : string.Join(", ", linkedNames))})"));
		}

		return Task.FromResult(ScoreResult.Pass(Name, linkedNames.Count == 0 ? "no owned links used" : $"linked: {string.Join(", ", linkedNames)}"));
	}
}

/// <summary>The agent loop must stay inside the configured iteration budget.</summary>
public sealed class IterationBudgetScorer : IEvalScorer {
	public string Name => "iteration-budget";

	public Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken) {
		if (trajectory.ChatModelCalls > context.MaxIterations) {
			return Task.FromResult(ScoreResult.Fail(Name, $"used {trajectory.ChatModelCalls} chat model calls, budget is {context.MaxIterations}"));
		}
		return Task.FromResult(ScoreResult.Pass(Name, $"{trajectory.ChatModelCalls}/{context.MaxIterations} chat model calls"));
	}
}

/// <summary>Conversation titles must be generated, short and not a copy of the user message.</summary>
public sealed class TitleScorer : IEvalScorer {
	public string Name => "title";

	public Task<ScoreResult> ScoreAsync(EvalCase evalCase, CaseTrajectory trajectory, EvalScoringContext context, CancellationToken cancellationToken) {
		var title = trajectory.Title?.Trim();
		if (string.IsNullOrWhiteSpace(title)) return Task.FromResult(ScoreResult.Fail(Name, "no conversation title was generated"));
		if (title.Length > 100) return Task.FromResult(ScoreResult.Fail(Name, $"title is {title.Length} characters: '{title}'"));
		if (title.EndsWith('.') || title.EndsWith('!')) return Task.FromResult(ScoreResult.Fail(Name, $"title should not end with punctuation: '{title}'"));

		var words = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		if (words.Length < 2) return Task.FromResult(ScoreResult.Fail(Name, $"title has {words.Length} word(s): '{title}'"));
		if (words.Length > 12) return Task.FromResult(ScoreResult.Fail(Name, $"title has {words.Length} words: '{title}'"));

		var fallbackTitle = evalCase.Message.Length > 50 ? evalCase.Message[..50] : evalCase.Message;
		if (title.StartsWith(fallbackTitle, StringComparison.OrdinalIgnoreCase)) {
			return Task.FromResult(ScoreResult.Fail(Name, $"title is still the truncated user message: '{title}'"));
		}

		return Task.FromResult(ScoreResult.Pass(Name, $"title: '{title}'"));
	}
}
