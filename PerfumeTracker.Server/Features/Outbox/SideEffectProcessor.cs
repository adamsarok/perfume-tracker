namespace PerfumeTracker.Server.Features.Outbox;

using MediatR;
using Microsoft.Build.Tasks;
using Microsoft.Extensions.Hosting;
using System;

/// <summary>
/// Async processing for updating mission progress
/// 1. Side effects are added to Outbox in db and Channel
/// 2. SideEffectProcessor immediately handles message from channel and marks the corresponding Outbox record processed
/// 3. OutboxService is responsible for error handling and cleanup. If the app shuts down or processing errors out, Outbox records a requeued 
/// </summary>
public class SideEffectProcessor(ISideEffectQueue queue, IServiceProvider serviceProvider, ILogger<SideEffectProcessor> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken)) {
			try {
				using var scope = serviceProvider.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>(); //this is a singleton service, there would be a lifetime mismatch without this
				var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
				context.Attach(message);
				await ProcessMessageAsync(message, publisher);
				await context.SaveChangesAsync(stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing side effect");
			}
		}
	}

	private async Task ProcessMessageAsync(OutboxMessage message, IPublisher publisher) {
		try {
			var eventType = Type.GetType(message.EventType);
			if (eventType == null) throw new InvalidOperationException($"Unknown event type: {message.EventType}");
			var evt = JsonSerializer.Deserialize(message.Payload, eventType);
			await publisher.Publish(evt);
			message.ProcessedAt = DateTime.UtcNow;
		} catch (Exception ex) {
			logger.LogError(ex, "Failed to process outbox message {MessageId}", message.Id);
			message.TryCount++;
			message.LastError = ex.Message;
			message.FailedAt = DateTime.UtcNow;
		}
	}
}