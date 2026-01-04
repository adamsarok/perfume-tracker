using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Users.Services;

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
	IEnumerable<PerfumeRecommendationStats> RecommendationStats,
	IEnumerable<RatingSpreadDto> RatingSpread
);
public record FavoritePerfumeDto(Guid Id, string House, string PerfumeName, decimal AverageRating, int WearCount);
public record FavoriteTagDto(Guid Id, string TagName, string Color, int WearCount, decimal TotalMl);
public record RatingSpreadDto(decimal Rating, int PerfumeCount, decimal TotalMl);

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
	IUserStatsService userStatsService) : IQueryHandler<UserStatsQuery, UserStatsResponse> {

	public async Task<UserStatsResponse> Handle(UserStatsQuery request, CancellationToken cancellationToken) {
		return await userStatsService.GetUserStats(cancellationToken);
	}
}