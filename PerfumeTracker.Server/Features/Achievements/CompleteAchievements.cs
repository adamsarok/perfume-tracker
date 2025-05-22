namespace PerfumeTracker.Server.Features.Achievements;

public class CompleteAchievements {
	public class PerfumeAddEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeAddedNotification> {
		public async Task Handle(PerfumeAddedNotification notification, CancellationToken cancellationToken) {
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
	public class PerfumeUpdatedEventHandler(PerfumeTrackerContext context, GetUserProfile getUserProfile) : INotificationHandler<PerfumeUpdatedNotification> {
		public async Task Handle(PerfumeUpdatedNotification notification, CancellationToken cancellationToken) {
			//no achievement yet?
			var userProfile = await getUserProfile.HandleAsync();
			userProfile.XP += 1;
			await context.SaveChangesAsync(cancellationToken);
		}
	}
}
