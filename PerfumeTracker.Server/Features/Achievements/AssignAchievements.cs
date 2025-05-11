using static PerfumeTracker.Server.Repo.PerfumeRepo;

namespace PerfumeTracker.Server.Features.Achievements;

public class AssignAchievements {
	public class PerfumeAddEventHandler(PerfumeTrackerContext context) : INotificationHandler<PerfumeAddedEvent> {
		public async Task Handle(PerfumeAddedEvent notification, CancellationToken cancellationToken) {
			var cnt = await context.Perfumes.CountAsync();

			var userProfile = await context.UserProfiles.FirstAsync();
			userProfile.XP += 10;
			await context.SaveChangesAsync(cancellationToken);
		}
	}
	public class PerfumeUpdatedEventHandler(PerfumeTrackerContext context) : INotificationHandler<PerfumeAddedEvent> {
		public Task Handle(PerfumeAddedEvent notification, CancellationToken cancellationToken) {
			//no achievement yet?
			return Task.CompletedTask;
		}
	}
}
