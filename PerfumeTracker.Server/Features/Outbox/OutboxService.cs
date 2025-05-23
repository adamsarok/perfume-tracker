using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Features.Missions;
using static PerfumeTracker.Server.Features.Missions.ProgressMissions;

namespace PerfumeTracker.Server.Features.Outbox;

public class OutboxService(IServiceProvider sp) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			using var scope = sp.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

			var messages = await context.OutboxMessages
				.Where(m => m.ProcessedAt == null)
				.OrderBy(m => m.CreatedAt)
				.Take(10)
				.ToListAsync();

			foreach (var message in messages) {
				var typ = Type.GetType(message.EventType);
				var evt = JsonSerializer.Deserialize(message.Payload, Type.GetType(message.EventType));
				await mediator.Publish(evt);
				message.ProcessedAt = DateTime.UtcNow;
			}

			await context.SaveChangesAsync();
			await Task.Delay(messages.Any() ? 1000 : 5000, stoppingToken);

			//TODO cleanup
		}
	}
}