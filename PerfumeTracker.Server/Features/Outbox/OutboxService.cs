
namespace PerfumeTracker.Server.Features.Outbox;

public class OutboxService(IServiceProvider sp, ILogger<OutboxService> logger, ISideEffectQueue queue) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
		while (!cancellationToken.IsCancellationRequested) {
			await ProcessMessages(cancellationToken);
			await Task.Delay(1000 * 60 * 60, cancellationToken);
		}
	}
	//TODO: maintenance - clean old messages, move retry > 5 to dead letter queue
	protected async Task ProcessMessages(CancellationToken cancellationToken) {
		try {
			using var scope = sp.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			var messages = await context.OutboxMessages
				.Where(m => m.ProcessedAt == null && m.TryCount < 5)
				.OrderBy(m => m.CreatedAt)
				.IgnoreQueryFilters() //processor runs for all users in the background
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