using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using PerfumeTracker.Server.Features.PerfumeEvents;
namespace PerfumeTracker.Server.Features.Streaks;

public class ProgressStreaks {
	public class StreakEventNotificationHandler(UpdateStreakProgressHandler updateStreakProgressHandler) : INotificationHandler<PerfumeEventAddedNotification> {
		public async Task Handle(PerfumeEventAddedNotification notification, CancellationToken cancellationToken) {
			await updateStreakProgressHandler.UpdateStreakProgress(cancellationToken, notification.UserId);
		}
	}
	public class UpdateStreakProgressHandler(PerfumeTrackerContext context,
		IHubContext<StreakProgressHub> streakProgressHub,
		ILogger<UpdateStreakProgressHandler> logger,
		IOptions<UserConfiguration> userConfiguration) {
		public async Task UpdateStreakProgress(CancellationToken cancellationToken, Guid userId) {
			var now = DateTime.UtcNow;
			var streak = await context.UserStreaks
				.IgnoreQueryFilters()
				.Where(um => um.UserId == userId && um.StreakEndAt == null)
				.FirstOrDefaultAsync(cancellationToken);
			var profile = await context.UserProfiles
				.IgnoreQueryFilters()
				.Where(um => um.Id == userId)
				.FirstAsync();
			StreakStatus status = StreakStatus.NoChange;
			if (streak != null) {
				int utcOffset = 0;
				try {
					var iana = string.IsNullOrWhiteSpace(profile.Timezone) ? "UTC" : profile.Timezone;
					TimeZoneInfo tzi2 = TimeZoneInfo.FindSystemTimeZoneById(iana);
					utcOffset = tzi2.GetUtcOffset(now).Hours;
				} catch (Exception ex) {
					logger.LogError(ex, "Timezone conversion failed");
				}
				status = GetStreakStatus(streak.LastProgressedAt, now, utcOffset);
				switch (status) {
					case StreakStatus.Ended:
						streak.StreakEndAt = streak.LastProgressedAt;
						break;
					case StreakStatus.Progress:
						streak.Progress++;
						streak.LastProgressedAt = DateTime.UtcNow;
						break;
				}
			}
			if (streak == null || status == StreakStatus.Ended) {
				var newStreak = new UserStreak() {
					StreakStartAt = now,
					LastProgressedAt = now,
					Progress = 1,
					UserId = userId,
				};
				context.UserStreaks.Add(newStreak);
				await context.SaveChangesAsync(cancellationToken);
				await SendStreakProgress(newStreak, cancellationToken);
			} else {
				await context.SaveChangesAsync(cancellationToken);
				await SendStreakProgress(streak, cancellationToken);
			}
		}
		public enum StreakStatus { Ended, Progress, NoChange }
		public StreakStatus GetStreakStatus(DateTime lastProgressDate, DateTime nowDate, int userUtcOffset) {
			var nowLocal = nowDate.AddHours(userUtcOffset).Date;
			var progressLocal = lastProgressDate.AddHours(userUtcOffset).Date;
			var diff = nowLocal - progressLocal;
			if (diff.TotalDays > userConfiguration.Value.StreakProtectionDays) return StreakStatus.Ended;
			if (nowLocal > progressLocal) return StreakStatus.Progress;
			return StreakStatus.NoChange;
		}
		private async Task SendStreakProgress(UserStreak streak, CancellationToken cancellationToken) {
			await streakProgressHub.Clients.User(streak.UserId.ToString()).SendAsync("ReceiveStreakProgress",
				streak.Adapt<UserStreakDto>(), cancellationToken
			);
		}
	}
	public class StreakProgressHub : Hub;
}
