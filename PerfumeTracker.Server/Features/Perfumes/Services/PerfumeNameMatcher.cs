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

		var normalizedSourceName = RemoveHousePrefix(NormalizePerfumeName(sourceName), normalizedSourceHouse);
		var normalizedTargetName = RemoveHousePrefix(NormalizePerfumeName(targetName), normalizedTargetHouse);

		if (normalizedSourceName == normalizedTargetName) return 1;

		return CalculateNameSimilarity(normalizedSourceName, normalizedTargetName);
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

	public static string RemoveHousePrefix(string perfumeName, string house) {
		if (string.IsNullOrWhiteSpace(perfumeName) || string.IsNullOrWhiteSpace(house)) return perfumeName;

		var normalizedName = perfumeName.Trim();
		var normalizedHouse = house.Trim();

		if (normalizedName.Equals(normalizedHouse, StringComparison.OrdinalIgnoreCase)) return normalizedName;

		if (normalizedName.StartsWith(normalizedHouse + " ", StringComparison.OrdinalIgnoreCase)) {
			return normalizedName[normalizedHouse.Length..].Trim();
		}

		var houseWords = normalizedHouse.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var nameWords = normalizedName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

		while (houseWords.Length > 0
			&& nameWords.Count > 0
			&& houseWords.Any(w => w.Equals(nameWords[0], StringComparison.OrdinalIgnoreCase))) {
			nameWords.RemoveAt(0);
		}

		return nameWords.Count == 0 ? normalizedName : string.Join(" ", nameWords);
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
