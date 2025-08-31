
using Mapster;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.PerfumeEvents;
public record DeletePerfumeEventCommand(Guid Id) : ICommand<PerfumeEventDownloadDto>;
public class DeletePerfumeEventEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapDelete("/api/perfume-events/{id}", async (Guid id, ISender sender, CancellationToken cancellationToken) => {
			await sender.Send(new DeletePerfumeEventCommand(id), cancellationToken);
			return Results.NoContent();
		}).WithTags("PerfumeWorns")
		.WithName("DeletePerfumeWorn")
		.RequireAuthorization(Policies.WRITE);
	}
}

public class DeletePerfumeEventHandler(PerfumeTrackerContext context) : ICommandHandler<DeletePerfumeEventCommand, PerfumeEventDownloadDto> {
	public async Task<PerfumeEventDownloadDto> Handle(DeletePerfumeEventCommand request, CancellationToken cancellationToken) {
		var w = await context.PerfumeEvents.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException("PerfumeEvents", request.Id);
		w.IsDeleted = true;
		await context.SaveChangesAsync(cancellationToken);
		return w.Adapt<PerfumeEventDownloadDto>();
	}
}