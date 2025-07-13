using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Features.PerfumeEvents;
using static PerfumeTracker.Server.Models.UserStreak;
namespace PerfumeTracker.Server.Features.Streaks;
public class ProgressStreaks {
	public class StreakEventNotificationHandler(UpdateStreakProgressHandler updateStreakProgressHandler) : INotificationHandler<PerfumeEventAddedNotification> {
		public async Task Handle(PerfumeEventAddedNotification notification, CancellationToken cancellationToken) {
			await updateStreakProgressHandler.UpdateStreakProgress(cancellationToken, notification.UserId);
		}
	}
	public class UpdateStreakProgressHandler(PerfumeTrackerContext context, IHubContext<StreakProgressHub> streakProgressHub) {
		const int streakProtectionDays = 1;
		public async Task UpdateStreakProgress(CancellationToken cancellationToken, Guid userId) {
			var now = DateTime.UtcNow;
			var userStreak = await context.UserStreaks
				.IgnoreQueryFilters()
				.Where(um => um.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);
			var profile = await context.UserProfiles.FirstAsync();
			bool streakEnded = false;
			if (userStreak != null) {
				TimeZoneInfo tzi2 = TimeZoneInfo.FindSystemTimeZoneById(profile.Timezone);
				var offset = tzi2.GetUtcOffset(DateTime.UtcNow);
				var dateUserLocal = DateTime.UtcNow.AddHours(offset.Hours);
				if ((dateUserLocal.Date - userStreak.LastProgressedAt.Date.AddDays(streakProtectionDays)).TotalDays > 1) {
					userStreak.StreakEndAt = userStreak.LastProgressedAt;
					streakEnded = true;
				} else {
					userStreak.Progress++;
				}
			}
			UserStreak newStreak = null;
			if (userStreak == null || streakEnded == true) {
				newStreak = new UserStreak() {
					StreakStartAt = DateTime.UtcNow,
					Progress = 1,
				};
				context.UserStreaks.Add(newStreak);
			}
			await context.SaveChangesAsync();
			await SendStreakProgress(newStreak ?? userStreak);

		}
		private async Task SendStreakProgress(UserStreak streak) {
			await streakProgressHub.Clients.User(streak.UserId.ToString()).SendAsync("ReceiveStreakProgress",
				streak.Adapt<UserStreakDto>()
			);
		}
	}
	public class StreakProgressHub : Hub;
}
