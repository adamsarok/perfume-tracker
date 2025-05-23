using PerfumeTracker.Server.Features.PerfumeEvents;

namespace PerfumeTracker.Server.Features.Missions;

public class CompleteMissions
{
    public class PerfumeAddEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeAddedNotification> {
		public async Task Handle(PerfumeAddedNotification notification, CancellationToken cancellationToken) {
			var perfumesAdded = await context.Perfumes.CountAsync();
			var userProfile = await getUserProfile.HandleAsync();

			//userProfile.XP += 10;
			await context.SaveChangesAsync(cancellationToken);
		}
	}

	public class PerfumeUpdatedEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeUpdatedNotification> {
		public async Task Handle(PerfumeUpdatedNotification notification, CancellationToken cancellationToken) {
			//no achievement yet?
			var userProfile = await getUserProfile.HandleAsync();

			//userProfile.XP += 1;
			await context.SaveChangesAsync(cancellationToken);
		}
	}

    public class PerfumeWornEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeEventAddedNotification> {
        public async Task Handle(PerfumeEventAddedNotification notification, CancellationToken cancellationToken) {
            var userProfile = await getUserProfile.HandleAsync();
            var perfumeId = notification.Dto.PerfumeId;

            await UpdateMissionProgress(context, userProfile.Id, MissionType.WearPerfumes, cancellationToken);

            var samePerfumeWornCount = await context.PerfumeEvents
                .CountAsync(x => x.PerfumeId == perfumeId && 
                                x.Type == PerfumeWorn.PerfumeEventType.Worn && 
                                x.EventDate >= DateTime.UtcNow.AddDays(-7));
            if (samePerfumeWornCount > 1) {
                await UpdateMissionProgress(context, userProfile.Id, MissionType.WearSamePerfume, cancellationToken);
            }

            var lastWorn = await context.PerfumeEvents
                .Where(x => x.PerfumeId == perfumeId && x.Type == PerfumeWorn.PerfumeEventType.Worn)
                .OrderByDescending(x => x.EventDate)
                .Skip(1)
                .FirstOrDefaultAsync();
            
            if (lastWorn != null && (DateTime.UtcNow - lastWorn.EventDate).TotalDays >= 30) {
                await UpdateMissionProgress(context, userProfile.Id, MissionType.UseUnusedPerfumes, cancellationToken);
            }

            var uniquePerfumesWorn = await context.PerfumeEvents
                .Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn && 
                           x.EventDate >= DateTime.UtcNow.AddDays(-7))
                .Select(x => x.PerfumeId)
                .Distinct()
                .CountAsync();
            
            if (uniquePerfumesWorn > 1) {
                await UpdateMissionProgress(context, userProfile.Id, MissionType.WearDifferentPerfumes, cancellationToken);
            }

            var activeNoteMission = await context.Missions
                .FirstOrDefaultAsync(m => m.Type == MissionType.WearNote && m.IsActive);
            
            if (activeNoteMission != null) {
                var perfumeTags = await context.PerfumeTags
                    .Where(pt => pt.PerfumeId == perfumeId)
                    .Select(pt => pt.Tag.TagName)
                    .ToListAsync();
                
                if (perfumeTags.Any()) {
                    await UpdateMissionProgress(context, userProfile.Id, MissionType.WearNote, cancellationToken);
                }
            }

            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public class RandomPerfumeEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<RandomPerfumeRepo.RandomPerfumeAddedEvent> {
        public async Task Handle(RandomPerfumeRepo.RandomPerfumeAddedEvent notification, CancellationToken cancellationToken) {
            var userProfile = await getUserProfile.HandleAsync();
            await UpdateMissionProgress(context, userProfile.Id, MissionType.GetRandoms, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    public class PerfumeTagsAddedEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeTagsAddedNotification> {
        public async Task Handle(PerfumeTagsAddedNotification notification, CancellationToken cancellationToken) {
            var userProfile = await getUserProfile.HandleAsync();
            await UpdateMissionProgress(context, userProfile.Id, MissionType.PerfumesTaggedPhotographed, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

	public class PerfumeRandomAcceptedNotificationHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeRandomAcceptedNotification> {
		public async Task Handle(PerfumeRandomAcceptedNotification notification, CancellationToken cancellationToken) {
			var userProfile = await getUserProfile.HandleAsync(); //TODO: remove all redundant userId calls
			await UpdateMissionProgress(context, userProfile.Id, MissionType.AcceptRandoms, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
		}
	}


	private static async Task UpdateMissionProgress(PerfumeTrackerContext context, int userId, MissionType type, CancellationToken cancellationToken, int progress = 1) {
        var now = DateTime.UtcNow;
        var activeMissions = await context.Missions
            .Where(m => m.IsActive && m.Type == type && m.StartDate <= now && m.EndDate > now)
            .ToListAsync(cancellationToken);

        foreach (var mission in activeMissions) {
            var userMission = await context.UserMissions //TODO this is duplicate
                .FirstOrDefaultAsync(um => um.UserId == userId && um.MissionId == mission.Id, cancellationToken);

            if (userMission == null) {
                userMission = new UserMission {
                    UserId = userId,
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

                    // Award XP to user
                    var userProfile = await context.UserProfiles.FindAsync(userId, cancellationToken);
                    if (userProfile != null) {
                        userProfile.XP += mission.XP;
                    }
                }
            }
        }
    }
}
