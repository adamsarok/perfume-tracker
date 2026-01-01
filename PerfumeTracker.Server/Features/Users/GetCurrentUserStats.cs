using PerfumeTracker.Server.Features.Common.Services;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Users;

public record UserStatsQuery() : IQuery<UserStatsResponse>;
public record UserStatsResponse(
	DateTime? StartDate,
	DateTime? LastWear,
	int WearCount,
	int PerfumesCount,
	decimal TotalPerfumesMl,
	decimal TotalPerfumesMlLeft,
	decimal MonthlyUsageMl,
	decimal YearlyUsageMl,
	IEnumerable<FavoritePerfumeDto> FavoritePerfumes,
	IEnumerable<FavoriteTagDto> FavoriteTags,
	int? CurrentStreak,
	int? BestStreak,
	long? XP,
	int? Level,
	decimal? XPMultiplier,
	IEnumerable<PerfumeRecommendationStats> RecommendationStats
);
public record FavoritePerfumeDto(Guid Id, string House, string PerfumeName, decimal AverageRating, int WearCount);
public record FavoriteTagDto(Guid Id, string TagName, string Color, int WearCount, decimal TotalMl);

public class GetCurrentUserStatsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/users/stats", (ISender sender, CancellationToken cancellationToken) => {
			return sender.Send(new UserStatsQuery(), cancellationToken);
		}).WithTags("Users")
			.WithName("GetCurrentUserStats")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetCurrentUserStatsHandler(
	PerfumeTrackerContext context,
	IPerfumeRecommender perfumeRecommender,
	IXPService xpService) : IQueryHandler<UserStatsQuery, UserStatsResponse> {

	public async Task<UserStatsResponse> Handle(UserStatsQuery request, CancellationToken cancellationToken) {
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
		var perfumes = await context.Perfumes.ToListAsync(cancellationToken);
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
		var tagGroups = await context.PerfumeTags
			.Include(pt => pt.Tag)
			.Include(pt => pt.Perfume)
			.Where(pt => pt.Perfume.WearCount > 0)
			.GroupBy(pt => new { pt.Tag.Id, pt.Tag.TagName, pt.Tag.Color })
			.OrderByDescending(pt => pt.Sum(s => s.Perfume.WearCount))
			.Take(10)
			.ToListAsync(cancellationToken);

		var favoriteTags = tagGroups.Select(g => new FavoriteTagDto(
				g.Key.Id,
				g.Key.TagName,
				g.Key.Color,
				g.Sum(pt => pt.Perfume.WearCount),
				g.Sum(pt => pt.Perfume.Ml)
			));

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

		// Get XP information
		long? xp = null;
		int? level = null;
		decimal? xpMultiplier = null;
		var xpResult = await xpService.GetUserXP(userId, cancellationToken);
		xp = xpResult.Xp;
		level = xpResult.Level;
		xpMultiplier = xpResult.XpMultiplier;


		// Get recommendation stats
		var recommendationStats = await perfumeRecommender.GetRecommendationStats(cancellationToken);

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
			XP: xp,
			Level: level,
			XPMultiplier: xpMultiplier,
			RecommendationStats: recommendationStats
		);
	}
}