namespace PerfumeTracker.Server.Features.Tags;
public record class UpdateTagCommand(Guid TagId, TagDto TagDto) : ICommand<TagDto>;
public class UpdateTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/tags/{id}", async (Guid id, TagDto dto, ISender sender) =>
			await sender.Send(new UpdateTagCommand(id, dto)))
			.WithTags("Tags")
			.WithName("UpdateTag")
			.RequireAuthorization(Policies.WRITE);
	}
}

public class UpdateTagHandler(PerfumeTrackerContext context) : ICommandHandler<UpdateTagCommand, TagDto> {
	public async Task<TagDto> Handle(UpdateTagCommand request, CancellationToken cancellationToken) {
		var tag = request.TagDto.Adapt<Tag>();
		if (tag == null) throw new MappingException();
		var find = await context
			.Tags
			.FindAsync(request.TagId);
		if (find == null) throw new NotFoundException();
		context.Entry(find).CurrentValues.SetValues(tag);
		await context.SaveChangesAsync();
		return find.Adapt<TagDto>();
	}
}