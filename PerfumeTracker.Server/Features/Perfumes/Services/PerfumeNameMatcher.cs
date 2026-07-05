namespace PerfumeTracker.Server.Features.Perfumes.Services;

public static class PerfumeNameMatcher {
	private static readonly Dictionary<string, string[]> PerfumeAbbreviations = new(StringComparer.OrdinalIgnoreCase) {
		{ "edp", ["eau de parfum"] },
		{ "edt", ["eau de toilette"] },
		{ "edc", ["eau de cologne"] }
	};

	public static string NormalizePerfumeName(string text) {
		var normalized = text.ToLower().Trim();

		foreach (var abbrev in PerfumeAbbreviations) {
			normalized = System.Text.RegularExpressions.Regex.Replace(
				normalized,
				$@"\b{System.Text.RegularExpressions.Regex.Escape(abbrev.Key)}\b",
				abbrev.Value[0],
				System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		}

		return normalized;
	}

	public static double CalculateSimilarity(string sourceHouse, string sourceName, string targetHouse, string targetName) {
		var normalizedSourceHouse = NormalizePerfumeName(sourceHouse);
		var normalizedTargetHouse = NormalizePerfumeName(targetHouse);

		if (!HousesMatch(normalizedSourceHouse, normalizedTargetHouse)) return 0;

		return CalculateNameSimilarity(NormalizePerfumeName(sourceName), NormalizePerfumeName(targetName));
	}

	public static bool HousesMatch(string house1, string house2) {
		if (house1 == house2) return true;
		if (house1.Contains(house2) || house2.Contains(house1)) return true;

		var words1 = house1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var words2 = house2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		if (words1.Length == 1 && words2.Any(w => w.Contains(words1[0]))) return true;
		if (words2.Length == 1 && words1.Any(w => w.Contains(words2[0]))) return true;

		return false;
	}

	private static double CalculateNameSimilarity(string sourceName, string targetName) {
		var sourceWords = sourceName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		var targetWords = targetName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

		if (sourceWords.Count == 0 || targetWords.Count == 0) return 0;

		var intersection = sourceWords.Intersect(targetWords).Count();
		var union = sourceWords.Union(targetWords).Count();
		var jaccardScore = (double)intersection / union;
		var coverageScore = (double)intersection / sourceWords.Count;

		return Math.Max(jaccardScore, coverageScore);
	}
}
