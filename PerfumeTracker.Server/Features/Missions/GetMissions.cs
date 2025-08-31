using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Missions;
public record UserMissionDto(Guid Id, 
	string Name, 
	string Description, 
	DateTime StartDate, 
	DateTime EndDate, 
	int XP, 
	MissionType MissionType, 
	int? RequiredCount, 
	int Progress,
	bool IsCompleted);
public record GetActiveMissionsQuery() : IQuery<List<UserMissionDto>>;
public class GetActiveMissionsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/missions/active", async (ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GetActiveMissionsQuery(), cancellationToken);
		})
			.WithTags("Missions")
			.WithName("GetActiveMissions")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetActiveMissionsHandler(PerfumeTrackerContext context)
		: IQueryHandler<GetActiveMissionsQuery, List<UserMissionDto>> {
	public async Task<List<UserMissionDto>> Handle(GetActiveMissionsQuery request, CancellationToken cancellationToken) {
		return await context
			.UserMissions
			.Include(x => x.Mission)
			.Where(x => x.Mission.EndDate >= DateTime.UtcNow)
			.Select(x => new UserMissionDto(x.Id, 
			x.Mission.Name, 
			x.Mission.Description, 
			x.Mission.StartDate, 
			x.Mission.EndDate,
			x.Mission.XP,
			x.Mission.Type,
			x.Mission.RequiredCount,
			x.Progress,
			x.IsCompleted)).ToListAsync(cancellationToken);
	}
}