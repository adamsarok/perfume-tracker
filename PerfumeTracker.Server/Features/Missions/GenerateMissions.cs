
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Missions;

public record GenerateMissionCommand : ICommand;
public class GenerateMissionsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/missions/generate", async (ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GenerateMissionCommand(), cancellationToken);
		})
			.WithTags("Missions")
			.WithName("GenerateMissions")
			.RequireAuthorization(Policies.READ); //should work for demo/read user
	}
}
public class GenerateMissions(PerfumeTrackerContext context) : ICommandHandler<GenerateMissionCommand> {
	public async Task<Unit> Handle(GenerateMissionCommand request, CancellationToken cancellationToken) {
		var now = DateTime.UtcNow;
		var missionIds = await context.Missions
			.AsNoTracking()
			.Where(m => m.IsActive && m.StartDate <= now && m.EndDate > now)
			.Select(m => m.Id)
			.ToListAsync(cancellationToken);

		var existing = await context.UserMissions
			.AsNoTracking()
			.Where(um => missionIds.Contains(um.MissionId))
			.Select(um => um.MissionId)
			.ToListAsync(cancellationToken);

		var toInsert = missionIds
			.Except(existing)
			.Select(mid => new UserMission {
				MissionId = mid,
				Progress = 0,
				IsCompleted = false
			});

		await context.UserMissions.AddRangeAsync(toInsert, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
		return new Unit();
	}
}
