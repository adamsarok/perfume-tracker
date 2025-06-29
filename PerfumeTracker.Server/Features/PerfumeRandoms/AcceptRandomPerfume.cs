

using PerfumeTracker.Server.Features.Outbox;

namespace PerfumeTracker.Server.Features.PerfumeRandoms;

public record AcceptRandomPerfumeCommand(int RandomsId) : ICommand<Unit>;
public class AcceptRandomPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/random-perfumes/{randomsId}", async (int randomsId, ISender sender) => {
			await sender.Send(new AcceptRandomPerfumeCommand(randomsId));
			return Results.Created();
		})
			.WithTags("PerfumeRandoms")
			.WithName("AcceptPerfumeSuggestion")
			.RequireAuthorization(Policies.WRITE);
	}
}
public record class RandomsAcceptedNotification(Guid UserId) : IUserNotification;
public class AcceptRandomPerfumeHandler(PerfumeTrackerContext context, ISideEffectQueue queue) : ICommandHandler<AcceptRandomPerfumeCommand, Unit> {
	public async Task<Unit> Handle(AcceptRandomPerfumeCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var perfumeRandom = await context.PerfumeRandoms.FindAsync(request.RandomsId);
		perfumeRandom.IsAccepted = true;
		var message = OutboxMessage.From(new RandomsAcceptedNotification(userId));
		context.OutboxMessages.Add(message);
		await context.SaveChangesAsync();
		queue.Enqueue(message);
		return Unit.Value;
	}
}
