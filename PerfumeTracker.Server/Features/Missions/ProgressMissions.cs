using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.PerfumeRandoms;

namespace PerfumeTracker.Server.Features.Missions;

public class ProgressMissions {
	public class PerfumeEventNotificationHandler(PerfumeTrackerContext context, UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeEventAddedNotification> {
		public async Task Handle(PerfumeEventAddedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearPerfumes, cancellationToken, notification.UserId);

			var samePerfumeWornCount = await context.PerfumeEvents
				.CountAsync(x => x.PerfumeId ==  notification.PerfumeId &&
								x.Type == PerfumeEvent.PerfumeEventType.Worn &&
								x.EventDate >= DateTime.UtcNow.AddDays(-7));
			if (samePerfumeWornCount > 1) {
				await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearSamePerfume, cancellationToken, notification.UserId);
			}

			var lastWorn = await context.PerfumeEvents
				.Where(x => x.PerfumeId == notification.PerfumeId && x.Type == PerfumeEvent.PerfumeEventType.Worn)
				.OrderByDescending(x => x.EventDate)
				.Skip(1)
				.FirstOrDefaultAsync();

			if (lastWorn != null && (DateTime.UtcNow - lastWorn.EventDate).TotalDays >= 30) {
				await updateMissionProgressHandler.UpdateMissionProgress(MissionType.UseUnusedPerfumes, cancellationToken, notification.UserId);
			}

			var uniquePerfumesWorn = await context.PerfumeEvents
				.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn &&
						   x.EventDate >= DateTime.UtcNow.AddDays(-7))
				.Select(x => x.PerfumeId)
				.Distinct()
				.CountAsync();

			if (uniquePerfumesWorn > 1) {
				await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearDifferentPerfumes, cancellationToken, notification.UserId);
			}

			var activeNoteMission = await context.Missions
				.FirstOrDefaultAsync(m => m.Type == MissionType.WearNote && m.IsActive);

			if (activeNoteMission != null) {
				var perfumeTags = await context.PerfumeTags
					.Where(pt => pt.PerfumeId == notification.PerfumeId)
					.Select(pt => pt.Tag.TagName)
					.ToListAsync();

				if (perfumeTags.Any()) {
					await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearNote, cancellationToken, notification.UserId);
				}
			}
		}
	}

	public class PerfumeTagsAddedNotificationHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeTagsAddedNotification> {
		public async Task Handle(PerfumeTagsAddedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.PerfumesTaggedPhotographed, cancellationToken, notification.UserId);
		}
	}

	public class PerfumeRandomAcceptedNotificationHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeRandomAcceptedNotification> {
		public async Task Handle(PerfumeRandomAcceptedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.AcceptRandoms, cancellationToken, notification.UserId);
		}
	}

	public class RandomPerfumeAddedNotificationHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<RandomPerfumeAddedNotification> {
		public async Task Handle(RandomPerfumeAddedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.GetRandoms, cancellationToken, notification.UserId);
		}
	}

	public class UpdateMissionProgressHandler(PerfumeTrackerContext context, IHubContext<MissionProgressHub> missionProgressHub) {
		//TODO refactor
		public async Task UpdateMissionProgress(MissionType type, CancellationToken cancellationToken, Guid userId, int progress = 1) {
			if (userId == Guid.Empty) return;
			var now = DateTime.UtcNow;
			var activeMissions = await context.Missions
				.Where(m => m.IsActive && m.Type == type && m.StartDate <= now && m.EndDate > now)
				.ToListAsync(cancellationToken);

			foreach (var mission in activeMissions) {
				var userMission = await context.UserMissions
					.IgnoreQueryFilters()
					.FirstOrDefaultAsync(um => um.MissionId == mission.Id && um.UserId == userId, cancellationToken);

				if (userMission == null) {
					userMission = new UserMission {
						MissionId = mission.Id,
						Progress = 0,
						IsCompleted = false,
						UserId = userId,
					};
					context.UserMissions.Add(userMission);
				} //TODO check if this was really needed?

				if (userMission != null && !userMission.IsCompleted) {
					userMission.Progress += progress;

					if (userMission.Progress >= mission.RequiredCount) {
						userMission.IsCompleted = true;
						userMission.CompletedAt = now;
					}
					await missionProgressHub.Clients.All.SendAsync("ReceiveMissionProgress",
						new UserMissionDto(
							Id: mission.Id,
							Progress: userMission.Progress,
							IsCompleted: userMission.IsCompleted,
							Description: mission.Description,
							EndDate: mission.EndDate,
							MissionType: mission.Type,
							Name: mission.Name,
							RequiredCount: mission.RequiredCount,
							StartDate: mission.StartDate,
							XP: mission.XP
					));
				}
			}
			await context.SaveChangesAsync(cancellationToken);
		}
	}
	public class MissionProgressHub : Hub;
}
