using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Features.Users.Services;

public class UserStatsService(PerfumeTrackerContext context, IPerfumeRecommender perfumeRecommender) : IUserStatsService {
	public async Task<UserStatsResponse> GetUserStats(CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();

		// Get basic wear statistics
		var wornEvents = await context.PerfumeEvents
			.Where(e => e.Type == PerfumeEvent.PerfumeEventType.Worn)
			.ToListAsync(cancellationToken);

		var startDate = wornEvents.Any() ? wornEvents.Min(e => e.EventDate) : (DateTime?)null;
		var lastWear = wornEvents.Any() ? wornEvents.Max(e => e.EventDate) : (DateTime?)null;
		var wearCount = wornEvents.Count;

		// Calculate usage per day, month, and year
		decimal monthlyUsageMl = 0;
		decimal yearlyUsageMl = 0;
		if (startDate.HasValue && wearCount > 0) {
			var daysSinceStart = (DateTime.UtcNow - startDate.Value).TotalDays;
			if (daysSinceStart > 0) {
				var totalUsageMl = wornEvents.Sum(e => Math.Abs(e.AmountMl));
				var dailyUsageMl = totalUsageMl / (decimal)daysSinceStart;
				monthlyUsageMl = dailyUsageMl * 30;
				yearlyUsageMl = dailyUsageMl * 365;
			}
		}

		// Get perfume count and total ML
		var perfumes = await context.Perfumes
			.Where(x => x.MlLeft > 0)
			.ToListAsync(cancellationToken);
		var perfumesCount = perfumes.Count;
		var totalPerfumesMl = perfumes.Sum(p => p.Ml);
		var totalPerfumesMlLeft = perfumes.Sum(p => p.MlLeft);

		// Get favorite perfumes (top 10 by rating, with at least 1 wear)
		var favoritePerfumes = await context.Perfumes
			.Where(p => p.AverageRating > 0 && p.WearCount > 0)
			.OrderByDescending(p => p.AverageRating)
			.ThenByDescending(p => p.WearCount)
			.Take(10)
			.Select(p => new FavoritePerfumeDto(
				p.Id,
				p.House,
				p.PerfumeName,
				p.AverageRating,
				p.WearCount
			))
			.ToListAsync(cancellationToken);

		// Get favorite tags (top 10 by wear count)
		var tagData = await context.PerfumeTags
			.Include(pt => pt.Tag)
			.Include(pt => pt.Perfume)
			.Where(pt => pt.Perfume.WearCount > 0)
			.Select(pt => new {
				pt.Tag.Id,
				pt.Tag.TagName,
				pt.Tag.Color,
				pt.Perfume.WearCount,
				pt.Perfume.Ml
			})
			.ToListAsync(cancellationToken);

		var favoriteTags = tagData
			.GroupBy(pt => new { pt.Id, pt.TagName, pt.Color })
			.Select(g => new FavoriteTagDto(
				g.Key.Id,
				g.Key.TagName,
				g.Key.Color,
				g.Sum(pt => pt.WearCount),
				g.Sum(pt => pt.Ml)
			))
			.OrderByDescending(tag => tag.WearCount)
			.Take(10);

		// Get streak information
		int? currentStreak = null;
		int? bestStreak = null;
		var activeStreak = await context.UserStreaks
			.Where(s => s.StreakEndAt == null)
			.FirstOrDefaultAsync(cancellationToken);
		if (activeStreak != null) {
			currentStreak = activeStreak.Progress;
		}
		var allStreaks = await context.UserStreaks.ToListAsync(cancellationToken);
		if (allStreaks.Any()) {
			bestStreak = allStreaks.Max(s => s.Progress);
		}

		// Get recommendation stats
		var recommendationStats = await perfumeRecommender.GetRecommendationStats(cancellationToken);

		// Get rating spread (rounded to 0.5 increments)
		var ratingSpread = await context.Perfumes
			.Where(p => p.AverageRating > 0)
			.Select(p => new {
				p.AverageRating,
				p.MlLeft
			})
			.ToListAsync(cancellationToken);

		var ratingSpreadGrouped = ratingSpread
			.GroupBy(p => Math.Round(p.AverageRating * 2, MidpointRounding.AwayFromZero) / 2)
			.Select(g => new RatingSpreadDto(
				g.Key,
				g.Count(),
				g.Sum(p => p.MlLeft)
			))
			.OrderByDescending(r => r.Rating)
			.ToList();

		return new UserStatsResponse(
			StartDate: startDate,
			LastWear: lastWear,
			WearCount: wearCount,
			PerfumesCount: perfumesCount,
			TotalPerfumesMl: totalPerfumesMl,
			TotalPerfumesMlLeft: totalPerfumesMlLeft,
			MonthlyUsageMl: monthlyUsageMl,
			YearlyUsageMl: yearlyUsageMl,
			FavoritePerfumes: favoritePerfumes,
			FavoriteTags: favoriteTags,
			CurrentStreak: currentStreak,
			BestStreak: bestStreak,
			RecommendationStats: recommendationStats,
			RatingSpread: ratingSpreadGrouped
		);
	}
}
