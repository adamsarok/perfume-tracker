namespace PerfumeTracker.Server.Features.Common;

public record OutboxAddedNotification(Guid UserId) : INotification;
public class NotifyOutbox(IPublisher publisher, ILogger<NotifyOutbox> logger) {
	public async Task NotifyOutboxAsync(Guid userId) {
		Task.Run(async () =>
		{
			try {
				await publisher.Publish(new OutboxAddedNotification(userId));
			} catch (Exception ex) {
				logger.LogError(ex, "OutboxAddedNotification failed");
			}
		});

	}
}
