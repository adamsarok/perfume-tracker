namespace PerfumeTracker.Server.Features.Achievements;

public class CompleteAchievements {
	public class PerfumeAddEventHandler(PerfumeTrackerContext context) : INotificationHandler<PerfumeAddedNotification> {
		public async Task Handle(PerfumeAddedNotification notification, CancellationToken cancellationToken) {
			var perfumesAdded = await context.Perfumes.CountAsync();
			var achievements = await context.Achievements
				.Where(x => x.MinPerfumesAdded != null && x.MinPerfumesAdded <= perfumesAdded)
				.Where(x => !context.UserAchievements
					.Select(ua => ua.AchievementId)
					.Contains(x.Id))
				.ToListAsync();
			if (achievements.Any()) {
				context.UserAchievements.AddRange(achievements.Select(x => new UserAchievement() {
					AchievementId = x.Id,
					UserId = PerfumeTrackerContext.DEFAULT_USERID
				}));
			}
			await context.SaveChangesAsync(cancellationToken);
		}
	}
}
