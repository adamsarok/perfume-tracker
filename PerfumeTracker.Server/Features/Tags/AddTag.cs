namespace PerfumeTracker.Server.Features.Tags;
public record class AddTagCommand(TagAddDto TagDto) : ICommand<TagDto>;
public class AddTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/tags", async (TagAddDto dto, ISender sender) => {
			var result = await sender.Send(new AddTagCommand(dto));
			return Results.CreatedAtRoute("GetTag", new { id = result.Id }, result);
		}).WithTags("Tags")
	   .WithName("AddTag")
	   .RequireAuthorization(Policies.WRITE);
	}
}

public class AddTagHandler(PerfumeTrackerContext context) : ICommandHandler<AddTagCommand, TagDto> {
	public async Task<TagDto> Handle(AddTagCommand request, CancellationToken cancellationToken) {
		var tag = request.TagDto.Adapt<Tag>();
		if (tag == null) throw new MappingException();
		context.Tags.Add(tag);
		await context.SaveChangesAsync();
		return tag.Adapt<TagDto>();
	}
}