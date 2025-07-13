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
	public class UpdateStreakProgressHandler(PerfumeTrackerContext context, IHubContext<StreakProgressHub> streakProgressHub, ILogger<UpdateStreakProgressHandler> logger)  {
		const int streakProtectionDays = 1;
		public async Task UpdateStreakProgress(CancellationToken cancellationToken, Guid userId) {
			var now = DateTime.UtcNow;
			var userStreak = await context.UserStreaks
				.IgnoreQueryFilters()
				.Where(um => um.UserId == userId)
				.FirstOrDefaultAsync(cancellationToken);
			var profile = await context.UserProfiles
				.IgnoreQueryFilters()
				.Where(um => um.Id == userId)
				.FirstAsync();
			bool streakEnded = false;
			if (userStreak != null) {
				int utcOffset = 0;
				try {
					var iana = string.IsNullOrWhiteSpace(profile.Timezone) ? "UTC" : profile.Timezone;
					TimeZoneInfo tzi2 = TimeZoneInfo.FindSystemTimeZoneById(iana);
					utcOffset = tzi2.GetUtcOffset(now).Hours;
				} catch (Exception ex) {
					logger.LogError(ex, "Timezone conversion failed");
				}
				var dateUserLocal = now.AddHours(utcOffset);
				if ((dateUserLocal.Date - userStreak.LastProgressedAt.Date.AddDays(streakProtectionDays)).TotalDays > 1) {
					userStreak.StreakEndAt = userStreak.LastProgressedAt;
					streakEnded = true;
				} else if (dateUserLocal.Date > userStreak.LastProgressedAt.AddHours(utcOffset).Date) {
					userStreak.Progress++;
				}
			}
			UserStreak newStreak = null;
			if (userStreak == null || streakEnded == true) {
				newStreak = new UserStreak() {
					StreakStartAt = DateTime.UtcNow,
					LastProgressedAt = DateTime.UtcNow,
					Progress = 1,
					UserId = userId,
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
