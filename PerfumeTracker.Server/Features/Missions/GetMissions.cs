﻿namespace PerfumeTracker.Server.Features.Missions;
public record UserMissionDto(int Id, 
	string Name, 
	string Description, 
	DateTime StartDate, 
	DateTime EndDate, 
	int XP, 
	MissionType MissionType, 
	int? RequiredCount, 
	string? RequiredName,
	int Progress,
	bool IsCompleted);
public record GetActiveMissionsQuery() : IQuery<List<UserMissionDto>>;
public class GetActiveMissionsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/missions/active", async (ISender sender) => {
			return await sender.Send(new GetActiveMissionsQuery());
		})
			.WithTags("Missions")
			.WithName("GetActiveMissions");
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
			x.Mission.RequiredId.ToString(), //TODO: get related name
			x.Progress,
			x.IsCompleted)).ToListAsync();
	}
}