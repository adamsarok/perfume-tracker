namespace PerfumeTracker.Server.Features.Outbox;

using PerfumeTracker.Server.Startup;
using System.Diagnostics;

public class OutboxBackgroundService(IServiceProvider sp, ILogger<OutboxBackgroundService> logger, ISideEffectQueue queue) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			await RetryMessages(stoppingToken);
			await Task.Delay(1000 * 60 * 60, stoppingToken); //this is just for re-queueing unprocessed messages
		}
	}
	protected async Task RetryMessages(CancellationToken cancellationToken) {
		using var activity = Diagnostics.ActivitySource.StartActivity("outbox.retry", ActivityKind.Internal);
		activity?.SetTag("job.name", nameof(OutboxBackgroundService));

		try {
			using var scope = sp.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			var messages = await context.OutboxMessages
				.Where(m => m.ProcessedAt == null && m.TryCount < 5)
				.OrderBy(m => m.CreatedAt)
				.IgnoreQueryFilters()
				.Take(1000)
				.ToListAsync(cancellationToken);

			activity?.SetTag("outbox.messages.count", messages.Count);

			foreach (var msg in messages) {
				queue.Enqueue(msg);
			}
		} catch (Exception ex) {
			activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
			activity?.SetTag("error.type", ex.GetType().FullName);
			logger.LogError(ex, "OutboxService failed");
		}
	}
}
