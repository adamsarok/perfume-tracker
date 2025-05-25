
namespace PerfumeTracker.Server.Features.PerfumeEvents;
public record DeletePerfumeEventCommand(Guid Id) : ICommand<Unit>;
public class DeletePerfumeEventEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapDelete("/api/perfume-events/{id}", async (Guid id, ISender sender) => {
			await sender.Send(new DeletePerfumeEventCommand(id));
			return Results.NoContent();
		}).WithTags("PerfumeWorns")
		.WithName("DeletePerfumeWorn");
	}
}

public class DeletePerfumeEventHandler(PerfumeTrackerContext context) : ICommandHandler<DeletePerfumeEventCommand, Unit> {
	public async Task<Unit> Handle(DeletePerfumeEventCommand request, CancellationToken cancellationToken) {
		var w = await context.PerfumeEvents.FindAsync(request.Id);
		if (w == null) throw new NotFoundException();
		w.IsDeleted = true;
		await context.SaveChangesAsync();
		return Unit.Value;
	}
}