using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Features.PerfumeEvents;

namespace PerfumeTracker.Server.Features.Missions;

public class ProgressMissions {
	public class PerfumeWornEventHandler(PerfumeTrackerContext context, UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeEventAddedNotification> {
		public async Task Handle(PerfumeEventAddedNotification notification, CancellationToken cancellationToken) {
			var perfumeId = notification.Dto.PerfumeId;

			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearPerfumes, cancellationToken);

			var samePerfumeWornCount = await context.PerfumeEvents
				.CountAsync(x => x.PerfumeId == perfumeId &&
								x.Type == PerfumeWorn.PerfumeEventType.Worn &&
								x.EventDate >= DateTime.UtcNow.AddDays(-7));
			if (samePerfumeWornCount > 1) {
				await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearSamePerfume, cancellationToken);
			}

			var lastWorn = await context.PerfumeEvents
				.Where(x => x.PerfumeId == perfumeId && x.Type == PerfumeWorn.PerfumeEventType.Worn)
				.OrderByDescending(x => x.EventDate)
				.Skip(1)
				.FirstOrDefaultAsync();

			if (lastWorn != null && (DateTime.UtcNow - lastWorn.EventDate).TotalDays >= 30) {
				await updateMissionProgressHandler.UpdateMissionProgress(MissionType.UseUnusedPerfumes, cancellationToken);
			}

			var uniquePerfumesWorn = await context.PerfumeEvents
				.Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn &&
						   x.EventDate >= DateTime.UtcNow.AddDays(-7))
				.Select(x => x.PerfumeId)
				.Distinct()
				.CountAsync();

			if (uniquePerfumesWorn > 1) {
				await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearDifferentPerfumes, cancellationToken);
			}

			var activeNoteMission = await context.Missions
				.FirstOrDefaultAsync(m => m.Type == MissionType.WearNote && m.IsActive);

			if (activeNoteMission != null) {
				var perfumeTags = await context.PerfumeTags
					.Where(pt => pt.PerfumeId == perfumeId)
					.Select(pt => pt.Tag.TagName)
					.ToListAsync();

				if (perfumeTags.Any()) {
					await updateMissionProgressHandler.UpdateMissionProgress(MissionType.WearNote, cancellationToken);
				}
			}
		}
	}

	public class RandomPerfumeEventHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<RandomPerfumeRepo.RandomPerfumeAddedEvent> {
		public async Task Handle(RandomPerfumeRepo.RandomPerfumeAddedEvent notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.GetRandoms, cancellationToken);
		}
	}

	public class PerfumeTagsAddedEventHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeTagsAddedNotification> {
		public async Task Handle(PerfumeTagsAddedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.PerfumesTaggedPhotographed, cancellationToken);
		}
	}

	public class PerfumeRandomAcceptedNotificationHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeRandomAcceptedNotification> {
		public async Task Handle(PerfumeRandomAcceptedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.AcceptRandoms, cancellationToken);
		}
	}

	public class UpdateMissionProgressHandler(PerfumeTrackerContext context, IHubContext<MissionProgressHub> missionProgressHub) {
		public async Task UpdateMissionProgress(MissionType type, CancellationToken cancellationToken, int progress = 1) {
			var now = DateTime.UtcNow;
			var activeMissions = await context.Missions
				.Where(m => m.IsActive && m.Type == type && m.StartDate <= now && m.EndDate > now)
				.ToListAsync(cancellationToken);

			foreach (var mission in activeMissions) {
				var userMission = await context.UserMissions //TODO this is duplicate
					.FirstOrDefaultAsync(um => um.MissionId == mission.Id, cancellationToken);

				if (userMission == null) {
					userMission = new UserMission {
						UserId = PerfumeTrackerContext.DefaultUserID,
						MissionId = mission.Id,
						Progress = 0,
						IsCompleted = false
					};
					context.UserMissions.Add(userMission);
				}

				if (!userMission.IsCompleted) {
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
							RequiredName: "TODO",
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
