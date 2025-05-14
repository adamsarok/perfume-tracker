using PerfumeTracker.Server.Features.UserProfiles;
using static PerfumeTracker.Server.Repo.PerfumeRepo;

namespace PerfumeTracker.Server.Features.Achievements;

public class AssignAchievements {
	public class PerfumeAddEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeAddedEvent> {
		public async Task Handle(PerfumeAddedEvent notification, CancellationToken cancellationToken) {
			var perfumesAdded = await context.Perfumes.CountAsync();
			var userProfile = await getUserProfile.HandleAsync();
			var achievements = await context.Achievements
				.Where(x => x.MinPerfumesAdded != null && x.MinPerfumesAdded <= perfumesAdded)
				.Where(x => !context.UserAchievements
					.Where(ua => ua.UserId == userProfile.Id)
					.Select(ua => ua.AchievementId)
					.Contains(x.Id))
				.ToListAsync();

			if (achievements.Any()) {
				context.UserAchievements.AddRange(achievements.Select(x => new UserAchievement() {
					AchievementId = x.Id,
					UserId = userProfile.Id
				}));
			}

			userProfile.XP += 10;
			await context.SaveChangesAsync(cancellationToken);
		}
	}
	public class PerfumeUpdatedEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeUpdatedEvent> {
		public async Task Handle(PerfumeUpdatedEvent notification, CancellationToken cancellationToken) {
			//no achievement yet?
			var userProfile = await getUserProfile.HandleAsync();
			userProfile.XP += 1;
			await context.SaveChangesAsync(cancellationToken);
		}
	}
}
