﻿
using PerfumeTracker.Server.Features.UserProfiles;

namespace PerfumeTracker.Server.Features.Missions;
public record GenerateMissionCommand : ICommand;
public class GenerateMissionsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/missions/generate", async (ISender sender) => {
			return await sender.Send(new GenerateMissionCommand());
		})
			.WithTags("Missions")
			.WithName("GenerateMissions");
	}
}
public class GenerateMissions(PerfumeTrackerContext context, GetUserProfile getUserProfile) : ICommandHandler<GenerateMissionCommand> {
	public async Task<Unit> Handle(GenerateMissionCommand request, CancellationToken cancellationToken) {
		var now = DateTime.UtcNow;
		var userProfile = await getUserProfile.HandleAsync(); //TODO: replace all unnecessary userProfile calls
		var activeMissions = await context.Missions
			.Where(m => m.IsActive && m.StartDate <= now && m.EndDate > now)
			.ToListAsync();

		foreach (var mission in activeMissions) {
			var userMission = await context.UserMissions
				.FirstOrDefaultAsync(um => um.UserId == userProfile.Id && um.MissionId == mission.Id);

			if (userMission == null) {
				userMission = new UserMission {
					UserId = userProfile.Id,
					MissionId = mission.Id,
					Progress = 0,
					IsCompleted = false
				};
				context.UserMissions.Add(userMission);
			}
		}
		await context.SaveChangesAsync(cancellationToken);
		return new Unit();
	}
}
