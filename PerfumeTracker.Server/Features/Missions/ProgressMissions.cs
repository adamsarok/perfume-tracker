using MediatR;
using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.PerfumeRandoms;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Features.Missions;

public class ProgressMissions {
	public class PerfumeEventNotificationHandler(PerfumeTrackerContext context, UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeEventAddedNotification> {
		public async Task Handle(PerfumeEventAddedNotification notification, CancellationToken cancellationToken) {
			var now = DateTime.UtcNow;
			var userMissions = await context.UserMissions
					.Include(x => x.Mission)
					.IgnoreQueryFilters()
				.Where(um => um.UserId == notification.UserId &&
					!um.IsDeleted &&
					!um.IsCompleted &&
					um.Mission.IsActive &&
					!um.Mission.IsDeleted &&
					um.Mission.StartDate <= now && 
					um.Mission.EndDate > now)
					.ToListAsync(cancellationToken);

			foreach (var m in userMissions) {
				switch (m.Mission.Type) {
					case MissionType.WearPerfumes:
						await updateMissionProgressHandler.UpdateMissionProgress(cancellationToken, m, m.Mission);
						break;
					case MissionType.WearSamePerfume:
						var samePerfumeWornCount = await context.PerfumeEvents
						.IgnoreQueryFilters()
						.CountAsync(x => x.UserId == notification.UserId &&
							x.PerfumeId == notification.PerfumeId &&
							x.Type == PerfumeEvent.PerfumeEventType.Worn &&
							x.EventDate >= m.Mission.StartDate);
						if (samePerfumeWornCount > 1) {
							await updateMissionProgressHandler.UpdateMissionProgress(cancellationToken, m, m.Mission);
						}
						break;
					case MissionType.UseUnusedPerfumes:
						var lastWorn = await context.PerfumeEvents
							.IgnoreQueryFilters()
							.Where(x => x.UserId == notification.UserId &&
								x.PerfumeId == notification.PerfumeId &&
								x.Type == PerfumeEvent.PerfumeEventType.Worn)
							.OrderByDescending(x => x.EventDate)
							.Skip(1)
							.FirstOrDefaultAsync();
						if (lastWorn != null && (now - lastWorn.EventDate).TotalDays >= 30) {
							await updateMissionProgressHandler.UpdateMissionProgress(cancellationToken, m, m.Mission);
						}
						break;
					case MissionType.WearDifferentPerfumes:
						var uniquePerfumesWorn = await context.PerfumeEvents
							.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn &&
									   x.EventDate >= m.Mission.StartDate &&
									   x.UserId == notification.UserId)
							.IgnoreQueryFilters()
							.Select(x => x.PerfumeId)
							.Distinct()
							.CountAsync();
						if (uniquePerfumesWorn > 0) {
							await updateMissionProgressHandler.UpdateMissionProgress(cancellationToken, m, m.Mission);
						}
						break;
					case MissionType.WearNote:
						var perfumeTags = await context.PerfumeTags
							.IgnoreQueryFilters()
							.Where(pt => pt.UserId == notification.UserId &&
								pt.PerfumeId == notification.PerfumeId)
							.Select(pt => pt.Tag.TagName)
							.ToListAsync();

						if (perfumeTags.Any()) {
							await updateMissionProgressHandler.UpdateMissionProgress(cancellationToken, m, m.Mission);
						}
						break;
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

	public class UpdateMissionProgressHandler(PerfumeTrackerContext context, IHubContext<MissionProgressHub> missionProgressHub, XPService xPService) {
		public async Task UpdateMissionProgress(MissionType type, CancellationToken cancellationToken, Guid userId, int? setExact = null) {
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
					var multiplier = await xPService.GetXPMultiplier(cancellationToken, userMission.UserId);
					userMission.XP_Awarded = (int)(userMission.Mission.XP * multiplier.XpMultiplier);
					userMission.IsCompleted = true;
					userMission.CompletedAt = now;
				}
				await missionProgressHub.Clients.User(userMission.UserId.ToString()).SendAsync("ReceiveMissionProgress",
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
	public class MissionProgressHub : Hub;
}
