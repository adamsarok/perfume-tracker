using MediatR;
using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.PerfumeRandoms;
namespace PerfumeTracker.Server.Features.Missions;

public class ProgressStreaks {
	public class StreakEventNotificationHandler(PerfumeTrackerContext context, UpdateStreakProgressHandler updateStreakProgressHandler) : INotificationHandler<PerfumeEventAddedNotification> {
		public async Task Handle(PerfumeEventAddedNotification notification, CancellationToken cancellationToken) {
			var now = DateTime.UtcNow;
			//TODO
		}
	}

	public class UpdateStreakProgressHandler(PerfumeTrackerContext context, IHubContext<StreakProgressHub> streakProgressHub) {
		public async Task UpdateStreakProgress(MissionType type, CancellationToken cancellationToken, Guid userId, int? setExact = null) {
			var now = DateTime.UtcNow;
			var userMission = await context.UserMissions
				.Include(x => x.Mission)
				.IgnoreQueryFilters()
				.Where(um => um.UserId == userId &&
					um.Mission.Type == type &&
					!um.IsDeleted &&
					!um.IsCompleted &&
					!um.Mission.IsDeleted &&
					um.Mission.IsActive &&
					um.Mission.StartDate <= now && um.Mission.EndDate > now)
				.FirstOrDefaultAsync(cancellationToken);
			if (userMission != null) await UpdateMissionProgress(cancellationToken, userMission, userMission.Mission, setExact);
		}
		public async Task UpdateMissionProgress(CancellationToken cancellationToken, UserMission userMission, Mission mission, int? setExact = null) {
			var now = DateTime.UtcNow;
			if (userMission != null && !userMission.IsCompleted) {
				userMission.Progress = setExact ?? userMission.Progress + 1;

				if (userMission.Progress >= mission.RequiredCount) {
					userMission.IsCompleted = true;
					userMission.CompletedAt = now;
				}
				await streakProgressHub.Clients.User(userMission.UserId.ToString()).SendAsync("ReceiveStreakProgress",
					new UserMissionDto(
						Id: mission.Id,
						Progress: (int)((float)userMission.Progress / (float)mission.RequiredCount * 100),
						IsCompleted: userMission.IsCompleted,
						Description: mission.Description,
						EndDate: mission.EndDate,
						MissionType: mission.Type,
						Name: mission.Name,
						RequiredCount: mission.RequiredCount,
						StartDate: mission.StartDate,
						XP: mission.XP
				));
				await context.SaveChangesAsync(cancellationToken);
			}
		}
	}
	public class StreakProgressHub : Hub;
}
