using System.Text.Json;

namespace PerfumeTracker.Evals.Reporting;

/// <summary>
/// Reads the case ids/statuses out of a previous results.json with a plain JSON reader so a
/// schema change in the report never breaks the regression check.
/// </summary>
public sealed class BaselineComparison {
	private readonly IReadOnlyDictionary<string, string> previous;
	private readonly string? missingPath;
	private BaselineComparison(IReadOnlyDictionary<string, string> previous, string? missingPath = null) {
		this.previous = previous;
		this.missingPath = missingPath;
	}

	// Load once before any output writes, including incremental digest reports.
	public static BaselineComparison Load(string baselinePath) {
		if (!File.Exists(baselinePath)) return new(new Dictionary<string, string>(), baselinePath);

		using var document = JsonDocument.Parse(File.ReadAllText(baselinePath));
		var previous = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (document.RootElement.TryGetProperty("cases", out var cases) && cases.ValueKind == JsonValueKind.Array) {
			foreach (var element in cases.EnumerateArray()) {
				var id = element.TryGetProperty("caseId", out var idElement) ? idElement.GetString() : null;
				var status = element.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
				if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(status)) previous[id] = status;
			}
		}

		return new(previous);
	}

	public static bool HasRegression(IReadOnlyList<string> comparison) => comparison.Any(line => line.StartsWith("REGRESSED:", StringComparison.Ordinal));

	public IReadOnlyList<string> Compare(IReadOnlyList<CaseResult> current) {
		if (missingPath != null) return [$"baseline file not found: {missingPath}"];
		var lines = new List<string>();
		foreach (var result in current) {
			if (result.Status == CaseStatus.Skipped) continue;
			var currentGood = result.Status == CaseStatus.Passed;
			if (previous.TryGetValue(result.CaseId, out var previousStatus)) {
				var previousGood = string.Equals(previousStatus, nameof(CaseStatus.Passed), StringComparison.OrdinalIgnoreCase);
				if (previousGood && !currentGood) lines.Add($"REGRESSED: {result.CaseId} ({previousStatus} -> {result.Status})");
				else if (!previousGood && currentGood) lines.Add($"fixed: {result.CaseId} ({previousStatus} -> {result.Status})");
			} else {
				lines.Add($"new: {result.CaseId} ({result.Status})");
			}
		}

		var removed = previous.Keys.Where(id => current.All(c => !string.Equals(c.CaseId, id, StringComparison.OrdinalIgnoreCase))).ToList();
		lines.AddRange(removed.Select(id => $"missing from this run: {id}"));

		return lines.Count == 0 ? ["no change versus baseline"] : lines;
	}
}
