using System.Text;

namespace PerfumeTracker.Server.Features.Users;

public static class UserStatsExtensions {
	public static string ToLlmString(this UserStatsResponse stats) {
		var sb = new StringBuilder();

		sb.AppendLine("USER PERFUME COLLECTION STATISTICS");
		sb.AppendLine("=================================");
		sb.AppendLine();

		// Overview
		sb.AppendLine("OVERVIEW:");
		sb.AppendLine($"- Total perfumes in collection: {stats.PerfumesCount}");
		sb.AppendLine($"- Total wears recorded: {stats.WearCount}");
		sb.AppendLine($"- First wear date: {(stats.StartDate.HasValue ? stats.StartDate.Value.ToString("yyyy-MM-dd") : "N/A")}");
		sb.AppendLine($"- Most recent wear: {(stats.LastWear.HasValue ? stats.LastWear.Value.ToString("yyyy-MM-dd") : "N/A")}");
		sb.AppendLine();

		// Collection Volume
		sb.AppendLine("COLLECTION VOLUME:");
		sb.AppendLine($"- Total perfume volume: {stats.TotalPerfumesMl:F1} ml");
		sb.AppendLine($"- Remaining volume: {stats.TotalPerfumesMlLeft:F1} ml");
		sb.AppendLine($"- Used volume: {(stats.TotalPerfumesMl - stats.TotalPerfumesMlLeft):F1} ml");
		sb.AppendLine($"- Monthly usage rate: {stats.MonthlyUsageMl:F2} ml/month");
		sb.AppendLine($"- Yearly usage rate: {stats.YearlyUsageMl:F2} ml/year");
		sb.AppendLine();

		// Streaks
		sb.AppendLine("USAGE STREAKS:");
		sb.AppendLine($"- Current daily wear streak: {stats.CurrentStreak?.ToString() ?? "0"} days");
		sb.AppendLine($"- Best streak ever: {stats.BestStreak?.ToString() ?? "N/A"} days");
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

		// Rating Distribution
		if (stats.RatingSpread.Any()) {
			sb.AppendLine("RATING DISTRIBUTION:");
			foreach (var rating in stats.RatingSpread) {
				sb.AppendLine($"- {rating.Rating:F1} stars: {rating.PerfumeCount} perfumes ({rating.TotalMl:F1} ml)");
			}
			sb.AppendLine();
		}

		// Recommendation Stats
		if (stats.RecommendationStats.Any()) {
			sb.AppendLine("RECOMMENDATION ACCEPTANCE RATES:");
			foreach (var rec in stats.RecommendationStats) {
				var acceptanceRate = rec.TotalRecommendations > 0 
					? (rec.AcceptedRecommendations * 100.0 / rec.TotalRecommendations) 
					: 0;
				sb.AppendLine($"- {rec.Strategy}: {rec.AcceptedRecommendations}/{rec.TotalRecommendations} accepted ({acceptanceRate:F1}%)");
			}
			sb.AppendLine();
		}

		return sb.ToString();
	}
}