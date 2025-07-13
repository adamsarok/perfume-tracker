using static PerfumeTracker.Server.Models.UserStreak;

namespace PerfumeTracker.Server.Features.Streaks;
public record UserStreakDto(Guid Id, 
	DateTime? CurrentStreakStart,
	int Progress,
	int BestStreakLength);

public record GetActiveStreaksQuery() : IQuery<List<UserStreakDto>>;
public class GetActiveStreaksEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/streaks/active", async (ISender sender) => {
			return await sender.Send(new GetActiveStreaksQuery());
		})
			.WithTags("Streaks")
			.WithName("GetActiveStreaks")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetActiveStreaksHandler(PerfumeTrackerContext context)
		: IQueryHandler<GetActiveStreaksQuery, List<UserStreakDto>> {
	public async Task<List<UserStreakDto>> Handle(GetActiveStreaksQuery request, CancellationToken cancellationToken) {
		int bestStreakLength = await context.UserStreaks.MaxAsync(x => x.Progress, cancellationToken);
		return await context
			.UserStreaks
			.Where(x => x.StreakEndAt == null)
			.Select(x => new UserStreakDto(
				x.Id,
				x.StreakStartAt,
				x.Progress,
				bestStreakLength
				)).ToListAsync(cancellationToken);
	}
}