using System.Text;

namespace PerfumeTracker.Server.Features.Users;

public static class UserStatsExtensions {
	public static string ToLlmString(this UserStatsResponse stats) {
		var sb = new StringBuilder();

		sb.AppendLine("USER PERFUME COLLECTION STATISTICS");
		sb.AppendLine("=================================");
		sb.AppendLine();

		// Favorite Perfumes
		if (stats.FavoritePerfumes.Any()) {
			sb.AppendLine("TOP RATED PERFUMES:");
			foreach (var perfume in stats.FavoritePerfumes) {
				sb.AppendLine($"- {perfume.House} - {perfume.PerfumeName}");
				sb.AppendLine($"  Rating: {perfume.AverageRating:F1}/10, Worn: {perfume.WearCount} times");
			}
			sb.AppendLine();
		}

		// Favorite Tags
		if (stats.FavoriteTags.Any()) {
			sb.AppendLine("MOST WORN NOTES/TAGS:");
			foreach (var tag in stats.FavoriteTags) {
				sb.AppendLine($"- {tag.TagName}: {tag.WearCount} wears, {tag.TotalMl:F1} ml total volume");
			}
			sb.AppendLine();
		}

		return sb.ToString();
	}
}