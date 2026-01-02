using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.Server.Features.Tags;
public record class UpdateTagCommand(Guid TagId, TagUploadDto Dto) : ICommand<TagDto>;
public class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand> {
	public UpdateTagCommandValidator() {
		RuleFor(x => x.Dto).SetValidator(new TagValidator());
	}
}
public class UpdateTagEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/tags/{id}", async (Guid id, TagUploadDto dto, ISender sender, CancellationToken cancellationToken) =>
			await sender.Send(new UpdateTagCommand(id, dto), cancellationToken))
			.WithTags("Tags")
			.WithName("UpdateTag")
			.RequireAuthorization(Policies.WRITE);
	}
}

public class UpdateTagHandler(PerfumeTrackerContext context) : ICommandHandler<UpdateTagCommand, TagDto> {
	public async Task<TagDto> Handle(UpdateTagCommand request, CancellationToken cancellationToken) {
		var find = await context
			.Tags
			.FindAsync([request.TagId], cancellationToken) ?? throw new NotFoundException("Tags", request.TagId);
		context.Entry(find).CurrentValues.SetValues(request.Dto);
		await context.SaveChangesAsync();
		return find.Adapt<TagDto>();
	}
}