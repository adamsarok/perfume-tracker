namespace PerfumeTracker.Server.Features.Outbox;

using Microsoft.Extensions.Hosting;
using PerfumeTracker.Server.Startup;
using System;
using System.Diagnostics;

/// <summary>
/// Async processing for updating mission progress
/// 1. Side effects are added to Outbox in db and Channel
/// 2. SideEffectProcessor immediately handles message from channel and marks the corresponding Outbox record processed
/// 3. OutboxService is responsible for error handling and cleanup. If the app shuts down or processing errors out, Outbox records a requeued 
/// </summary>
public class SideEffectBackgroundService(ISideEffectQueue queue, IServiceProvider serviceProvider, ILogger<SideEffectBackgroundService> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await foreach (var message in queue.Reader.ReadAllAsync(stoppingToken)) {
			try {
				using var scope = serviceProvider.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>(); //Create scope because SideEffectProcessor is singleton but DbContext is scoped
				var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
				context.Attach(message);
				await ProcessMessageAsync(message, publisher, stoppingToken);
				await context.SaveChangesAsync(stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing side effect");
			}
		}
	}

	private async Task ProcessMessageAsync(OutboxMessage message, IPublisher publisher, CancellationToken cancellationToken) {
		try {
			using var activity = Diagnostics.ActivitySource.StartActivity(
				  "outbox.process",
				  ActivityKind.Consumer,
				  CreateParentContext(message));

			activity?.SetTag("outbox.message.id", message.Id);
			activity?.SetTag("outbox.event_type", message.EventType);

			logger.LogInformation(
				"Processing outbox message {OutboxMessageId} of type {EventType}",
				message.Id,
				message.EventType);

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

	private static ActivityContext CreateParentContext(OutboxMessage message) {
		if (message.TraceId is not { Length: 32 } traceIdValue ||
			message.SpanId is not { Length: 16 } spanIdValue) {
			return default;
		}

		try {
			return new ActivityContext(
				ActivityTraceId.CreateFromString(traceIdValue.AsSpan()),
				ActivitySpanId.CreateFromString(spanIdValue.AsSpan()),
				ActivityTraceFlags.Recorded,
				traceState: null,
				isRemote: true);
		} catch (ArgumentException) {
			return default;
		}
	}
}
