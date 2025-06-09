

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
public record class RandomsAcceptedNotification() : INotification;
public class AcceptRandomPerfumeHandler(PerfumeTrackerContext context) : ICommandHandler<AcceptRandomPerfumeCommand, Unit> {
	public async Task<Unit> Handle(AcceptRandomPerfumeCommand request, CancellationToken cancellationToken) {
		var perfumeRandom = await context.PerfumeRandoms.FindAsync(request.RandomsId);
		perfumeRandom.IsAccepted = true;
		context.OutboxMessages.Add(OutboxMessage.From(new RandomsAcceptedNotification()));
		await context.SaveChangesAsync();
		return Unit.Value;
	}
}
