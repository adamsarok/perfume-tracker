namespace PerfumeTracker.Server.Features.Outbox;

using Microsoft.Extensions.Hosting;
using System;

/// <summary>
/// Async processing for updating mission progress
/// 1. Side effects are added to Outbox in db and Channel
/// 2. SideEffectProcessor immediately handles message from channel and marks the corresponding Outbox record processed
/// 3. OutboxService is responsible for error handling and cleanup. If the app shuts down or processing errors out, Outbox records a requeued 
/// </summary>
public class SideEffectProcessor(ISideEffectQueue queue, IServiceProvider serviceProvider, ILogger<SideEffectProcessor> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken cancellationToken) {
		await foreach (var message in queue.Reader.ReadAllAsync(cancellationToken)) {
			try {
				using var scope = serviceProvider.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>(); //Create scope because SideEffectProcessor is singleton but DbContext is scoped
				var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
				context.Attach(message);
				await ProcessMessageAsync(message, publisher, cancellationToken);
				await context.SaveChangesAsync(cancellationToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing side effect");
			}
		}
	}

	private async Task ProcessMessageAsync(OutboxMessage message, IPublisher publisher, CancellationToken cancellationToken) {
		try {
			var eventType = Type.GetType(message.EventType);
			if (eventType == null) throw new InvalidOperationException($"Unknown event type: {message.EventType}");
			var evt = JsonSerializer.Deserialize(message.Payload, eventType);
			if (evt == null) throw new InvalidOperationException($"Failed to deserialize payload for event type: {message.EventType}");
			await publisher.Publish(evt, cancellationToken);
			message.ProcessedAt = DateTime.UtcNow;
		} catch (Exception ex) {
			logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
			message.TryCount++;
			message.LastError = ex.Message;
			message.FailedAt = DateTime.UtcNow;
		}
	}
}