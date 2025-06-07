
namespace PerfumeTracker.Server.Features.Perfumes;
public record UpdatePerfumeGuidCommand(ImageGuidDto Dto) : ICommand<PerfumeDto>;
public class UpdatePerfumeImageGuidEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/perfumes/imageguid", async (ImageGuidDto dto, ISender sender) =>
			await sender.Send(new UpdatePerfumeGuidCommand(dto)))
			.WithTags("Perfumes")
			.WithName("UpdatePerfumeImageGuid")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class UpdatePerfumeImageGuidHandler(PerfumeTrackerContext context) 
	: ICommandHandler<UpdatePerfumeGuidCommand, PerfumeDto> {
	public async Task<PerfumeDto> Handle(UpdatePerfumeGuidCommand request, CancellationToken cancellationToken) {
		var find = await context.Perfumes.FindAsync(request.Dto.ParentObjectId);
		if (find == null) throw new NotFoundException();
		find.ImageObjectKey = request.Dto.ImageObjectKey;
		await context.SaveChangesAsync();
		return find.Adapt<PerfumeDto>();
	}
}