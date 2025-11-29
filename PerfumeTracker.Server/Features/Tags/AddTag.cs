using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Tags;

public record class AddTagCommand(TagUploadDto Dto) : ICommand<TagDto>;
public class AddTagCommandValidator : AbstractValidator<AddTagCommand> {
	public AddTagCommandValidator() {
		RuleFor(x => x.Dto).SetValidator(new TagValidator());
	}
}
public class AddTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/tags", async (TagUploadDto dto, ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(new AddTagCommand(dto), cancellationToken);
			return Results.CreatedAtRoute("GetTag", new { id = result.Id }, result);
		}).WithTags("Tags")
	   .WithName("AddTag")
	   .RequireAuthorization(Policies.WRITE);
	}
}

public class AddTagHandler(PerfumeTrackerContext context) : ICommandHandler<AddTagCommand, TagDto> {
	public async Task<TagDto> Handle(AddTagCommand request, CancellationToken cancellationToken) {
		var tag = request.Dto.Adapt<Tag>();
		if (tag == null) throw new MappingException();
		context.Tags.Add(tag);
		await context.SaveChangesAsync(cancellationToken);
		return tag.Adapt<TagDto>();
	}
}