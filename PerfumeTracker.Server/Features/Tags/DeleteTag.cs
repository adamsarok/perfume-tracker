
namespace PerfumeTracker.Server.Features.Tags;
public record DeleteTagCommand(Guid TagId) : ICommand;
public class DeleteTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapDelete("/api/tags/{id}", async (Guid id, ISender sender) => {
			await sender.Send(new DeleteTagCommand(id));
			return Results.NoContent();
		}).WithTags("Tags")
		   .WithName("DeleteTag")
		   .RequireAuthorization(Policies.WRITE);
	}
}
public class DeleteTagHandler(PerfumeTrackerContext context) : ICommandHandler<DeleteTagCommand, Unit> {
	public async Task<Unit> Handle(DeleteTagCommand request, CancellationToken cancellationToken) {
		var tag = await context.Tags.FindAsync(request.TagId);
		if (tag == null) throw new NotFoundException();
		tag.IsDeleted = true;
		await context.SaveChangesAsync();
		return Unit.Value;
	}
}