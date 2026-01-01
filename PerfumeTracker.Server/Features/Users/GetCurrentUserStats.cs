using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Users;

public record UserStatsQuery() : IQuery<UserStatsResponse>;
public record UserStatsResponse(IEnumerable<PerfumeRecommendationStats> RecommendationStats);
public class GetCurrentUserStatsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/users/stats", (ISender sender, CancellationToken cancellationToken) => {
			return sender.Send(new UserStatsQuery(), cancellationToken);
		}).WithTags("Users")
			.WithName("GetCurrentUserStats")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetCurrentUserStatsHandler(PerfumeTrackerContext context, IPerfumeRecommender perfumeRecommender) : IQueryHandler<UserStatsQuery, UserStatsResponse> {
	public async Task<UserStatsResponse> Handle(UserStatsQuery request, CancellationToken cancellationToken) {
		var recommendationStats = await perfumeRecommender.GetRecommendationStats(cancellationToken);
		return new UserStatsResponse(RecommendationStats: recommendationStats);
	}
}