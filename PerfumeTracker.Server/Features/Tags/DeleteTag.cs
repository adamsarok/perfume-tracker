
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Tags;
public record DeleteTagCommand(Guid TagId) : ICommand<TagDto>;
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
public class DeleteTagHandler(PerfumeTrackerContext context) : ICommandHandler<DeleteTagCommand, TagDto> {
	public async Task<TagDto> Handle(DeleteTagCommand request, CancellationToken cancellationToken) {
		var tag = await context.Tags.FindAsync(request.TagId) ?? throw new NotFoundException("Tags", request.TagId);
		tag.IsDeleted = true;
		await context.SaveChangesAsync();
		return tag.Adapt<TagDto>();
	}
}