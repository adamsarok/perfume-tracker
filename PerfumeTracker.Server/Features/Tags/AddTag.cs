namespace PerfumeTracker.Server.Features.Tags;
public record class AddTagCommand(TagDto TagDto) : ICommand<TagDto>;
public class AddTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/tags", async (TagDto dto, TagRepo repo) => {
			var result = await repo.AddTag(dto);
			return Results.CreatedAtRoute("GetTag", new { id = result.Id }, result);
		}).WithTags("Tags")
	   .WithName("AddTag");
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