using System.Text.Json;

namespace PerfumeTracker.Evals.Cases;

public static class EvalCaseLoader {
	private static readonly JsonSerializerOptions SerializerOptions = new() {
		PropertyNameCaseInsensitive = true,
		ReadCommentHandling = JsonCommentHandling.Skip,
		AllowTrailingCommas = true
	};

	public static async Task<List<EvalCase>> LoadAsync(string path, string? filter, CancellationToken cancellationToken) {
		if (!File.Exists(path)) throw new FileNotFoundException($"Eval case file not found: {path}", path);

		var json = await File.ReadAllTextAsync(path, cancellationToken);
		var dataset = JsonSerializer.Deserialize<EvalDataset>(json, SerializerOptions)
			?? throw new InvalidDataException($"Could not parse eval case file: {path}");

		var cases = dataset.Cases;
		Validate(cases);

		if (!string.IsNullOrWhiteSpace(filter)) {
			cases = cases
				.Where(c => c.Id.Contains(filter, StringComparison.OrdinalIgnoreCase)
					|| c.Category.Contains(filter, StringComparison.OrdinalIgnoreCase))
				.ToList();
			if (cases.Count == 0) throw new InvalidOperationException($"Filter '{filter}' matched no cases.");
		}

		return cases;
	}

	private static void Validate(List<EvalCase> cases) {
		if (cases.Count == 0) throw new InvalidDataException("Eval case file contains no cases.");
		var duplicates = cases.GroupBy(c => c.Id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
		if (duplicates.Count > 0) throw new InvalidDataException($"Duplicate case ids: {string.Join(", ", duplicates)}");
		foreach (var c in cases) {
			if (string.IsNullOrWhiteSpace(c.Id)) throw new InvalidDataException("Every case needs an id.");
			if (string.IsNullOrWhiteSpace(c.Message)) throw new InvalidDataException($"Case '{c.Id}' needs a message.");
			if (c.Judge && string.IsNullOrWhiteSpace(c.Rubric)) throw new InvalidDataException($"Case '{c.Id}' enables the judge but has no rubric.");
		}
	}
}
