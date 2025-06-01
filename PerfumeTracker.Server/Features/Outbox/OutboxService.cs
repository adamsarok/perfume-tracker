
namespace PerfumeTracker.Server.Features.Outbox;

public class OutboxService(IServiceProvider sp, ILogger<OutboxService> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			try {
				using var scope = sp.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
				var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

				var messages = await context.OutboxMessages
					.Where(m => m.ProcessedAt == null && m.TryCount < 5)
					.OrderBy(m => m.CreatedAt)
					.Take(10)
					.ToListAsync();

				foreach (var message in messages) {
					try {
						var eventType = Type.GetType(message.EventType);
						if (eventType == null) throw new InvalidOperationException($"Unknown event type: {message.EventType}");
						var evt = JsonSerializer.Deserialize(message.Payload, eventType);
						await mediator.Publish(evt);
						message.ProcessedAt = DateTime.UtcNow;
					} catch (Exception ex) {
						logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
						message.TryCount++;
						message.LastError = ex.Message;
						message.FailedAt = DateTime.UtcNow;
					}
				}

				await context.SaveChangesAsync();
				await Task.Delay(messages.Any() ? 1000 : 5000, stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "OutboxService failed");
			}
		}
	}
	//TODO: maintenance - clean old messages, move retry > 5 to dead letter queue
}