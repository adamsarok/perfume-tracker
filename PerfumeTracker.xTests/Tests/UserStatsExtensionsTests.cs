using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Users;

namespace PerfumeTracker.xTests.Tests;

public class UserStatsExtensionsTests {

	[Fact]
	public void ToLlmString_WithFavorites_FormatsCorrectly() {
		var stats = new UserStatsResponse(
			StartDate: DateTime.UtcNow.AddDays(-30),
			LastWear: DateTime.UtcNow,
			WearCount: 15,
			PerfumesCount: 5,
			TotalPerfumesMl: 250m,
			TotalPerfumesMlLeft: 180m,
			MonthlyUsageMl: 10m,
			YearlyUsageMl: 120m,
			FavoritePerfumes: new[] {
				new FavoritePerfumeDto(Guid.NewGuid(), "House1", "Big Citrus", 9.5m, 10)
			},
			FavoriteTags: new[] {
				new FavoriteTagDto(Guid.NewGuid(), "Woody", "#8B4513", 20, 100m)
			},
			CurrentStreak: 5,
			BestStreak: 10,
			RecommendationStats: Enumerable.Empty<PerfumeRecommendationStats>(),
			RatingSpread: Enumerable.Empty<RatingSpreadDto>()
		);

		var result = stats.ToLlmString();

		Assert.Contains("USER PERFUME COLLECTION STATISTICS", result);
		Assert.Contains("TOP RATED PERFUMES", result);
		Assert.Contains("House1 - Big Citrus", result);
		Assert.Contains("MOST WORN NOTES/TAGS", result);
		Assert.Contains("Woody", result);
		Assert.Contains("20 wears", result);
	}

	[Fact]
	public void ToLlmString_EmptyFavorites_DoesNotIncludeSections() {
		var stats = new UserStatsResponse(
			StartDate: null,
			LastWear: null,
			WearCount: 0,
			PerfumesCount: 0,
			TotalPerfumesMl: 0m,
			TotalPerfumesMlLeft: 0m,
			MonthlyUsageMl: 0m,
			YearlyUsageMl: 0m,
			FavoritePerfumes: Enumerable.Empty<FavoritePerfumeDto>(),
			FavoriteTags: Enumerable.Empty<FavoriteTagDto>(),
			CurrentStreak: null,
			BestStreak: null,
			RecommendationStats: Enumerable.Empty<PerfumeRecommendationStats>(),
			RatingSpread: Enumerable.Empty<RatingSpreadDto>()
		);

		var result = stats.ToLlmString();

		Assert.Contains("USER PERFUME COLLECTION STATISTICS", result);
		Assert.DoesNotContain("TOP RATED PERFUMES", result);
		Assert.DoesNotContain("MOST WORN NOTES/TAGS", result);
	}
}
