namespace PerfumeTracker.Server.Services.Outbox;

public class OutboxRetryService(IServiceProvider sp, ILogger<OutboxRetryService> logger, ISideEffectQueue queue) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			await RetryMessages(stoppingToken);
			await Task.Delay(1000 * 60 * 60, stoppingToken); //this is just for re-queueing unprocessed messages
		}
	}
	protected async Task RetryMessages(CancellationToken cancellationToken) {
		try {
			using var scope = sp.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			var messages = await context.OutboxMessages
				.Where(m => m.ProcessedAt == null && m.TryCount < 5)
				.OrderBy(m => m.CreatedAt)
				.IgnoreQueryFilters()
				.Take(1000)
				.ToListAsync(cancellationToken);

			foreach (var msg in messages) {
				queue.Enqueue(msg);
			}
		} catch (Exception ex) {
			logger.LogError(ex, "OutboxService failed");
		}
	}
}