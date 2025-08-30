using MediatR;
using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.PerfumeRandoms;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Services.Missions;

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
						await updateMissionProgressHandler.UpdateMissionProgress(m, m.Mission, cancellationToken);
						break;
					case MissionType.WearSamePerfume:
						var samePerfumeWornCount = await context.PerfumeEvents
						.IgnoreQueryFilters()
						.CountAsync(x => x.UserId == notification.UserId &&
							x.PerfumeId == notification.PerfumeId &&
							x.Type == PerfumeEvent.PerfumeEventType.Worn &&
							x.EventDate >= m.Mission.StartDate, cancellationToken);
						if (samePerfumeWornCount > 1) await updateMissionProgressHandler.UpdateMissionProgress(m, m.Mission, cancellationToken);
						break;
					case MissionType.UseUnusedPerfumes:
						var lastWorn = await context.PerfumeEvents
							.IgnoreQueryFilters()
							.Where(x => x.UserId == notification.UserId &&
								x.PerfumeId == notification.PerfumeId &&
								x.Type == PerfumeEvent.PerfumeEventType.Worn)
							.OrderByDescending(x => x.EventDate)
							.Skip(1)
							.FirstOrDefaultAsync(cancellationToken);
						if (lastWorn != null && (now - lastWorn.EventDate).TotalDays >= 30) await updateMissionProgressHandler.UpdateMissionProgress(m, m.Mission, cancellationToken);
						break;
					case MissionType.WearDifferentPerfumes:
						var uniquePerfumesWorn = await context.PerfumeEvents
							.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn &&
									   x.EventDate >= m.Mission.StartDate &&
									   x.UserId == notification.UserId)
							.IgnoreQueryFilters()
							.Select(x => x.PerfumeId)
							.Distinct()
							.CountAsync(cancellationToken);
						if (uniquePerfumesWorn > 0) await updateMissionProgressHandler.UpdateMissionProgress(m, m.Mission, cancellationToken);
						break;
					case MissionType.WearNote:
						var perfumeTags = await context.PerfumeTags
							.IgnoreQueryFilters()
							.Where(pt => pt.UserId == notification.UserId &&
								pt.PerfumeId == notification.PerfumeId)
							.Select(pt => pt.Tag.TagName)
							.ToListAsync(cancellationToken);

						if (perfumeTags.Count > 0) await updateMissionProgressHandler.UpdateMissionProgress(m, m.Mission, cancellationToken);
						break;
				}
			}
		}
	}

	public class PerfumeTagsAddedNotificationHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeTagsAddedNotification> {
		public async Task Handle(PerfumeTagsAddedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.PerfumesTaggedPhotographed, notification.UserId, cancellationToken);
		}
	}

	public class PerfumeRandomAcceptedNotificationHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<PerfumeRandomAcceptedNotification> {
		public async Task Handle(PerfumeRandomAcceptedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.AcceptRandoms, notification.UserId, cancellationToken);
		}
	}

	public class RandomPerfumeAddedNotificationHandler(UpdateMissionProgressHandler updateMissionProgressHandler) : INotificationHandler<RandomPerfumeAddedNotification> {
		public async Task Handle(RandomPerfumeAddedNotification notification, CancellationToken cancellationToken) {
			await updateMissionProgressHandler.UpdateMissionProgress(MissionType.GetRandoms, notification.UserId, cancellationToken);
		}
	}

	public class UpdateMissionProgressHandler(PerfumeTrackerContext context, IHubContext<MissionProgressHub> missionProgressHub, XPService xPService) {
		public async Task UpdateMissionProgress(MissionType type, Guid userId, CancellationToken cancellationToken) {
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
			if (userMission != null) await UpdateMissionProgress(userMission, userMission.Mission, cancellationToken);
		}
		public async Task UpdateMissionProgress(UserMission userMission, Mission mission, CancellationToken cancellationToken) {
			var now = DateTime.UtcNow;
			if (userMission != null && !userMission.IsCompleted) {
				userMission.Progress += 1;

				if (userMission.Progress >= mission.RequiredCount) {
					var multiplier = await xPService.GetXPMultiplier(cancellationToken, userMission.UserId);
					userMission.XP_Awarded = (int)(userMission.Mission.XP * multiplier.XpMultiplier);
					userMission.IsCompleted = true;
					userMission.CompletedAt = now;
				}
				await missionProgressHub.Clients.User(userMission.UserId.ToString()).SendAsync("ReceiveMissionProgress",
					new UserMissionDto(
						Id: mission.Id,
						Progress: (int)(userMission.Progress / (float)mission.RequiredCount * 100),
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
