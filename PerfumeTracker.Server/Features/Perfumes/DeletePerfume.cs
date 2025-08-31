using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Perfumes;
public record DeletePerfumeCommand(Guid Id) : ICommand<PerfumeDto>;
public class DeletePerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapDelete("/api/perfumes/{id}", async (Guid id, ISender sender, CancellationToken cancellationToken) => {
			await sender.Send(new DeletePerfumeCommand(id), cancellationToken);
			return Results.NoContent();
		}).WithTags("Perfumes")
			.WithName("DeletePerfume")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class DeletePerfumeHandler(PerfumeTrackerContext context) : ICommandHandler<DeletePerfumeCommand, PerfumeDto>  {
	public async Task<PerfumeDto> Handle(DeletePerfumeCommand request, CancellationToken cancellationToken) {
		var perfume = await context.Perfumes.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException("Perfumes", request.Id);
		perfume.IsDeleted = true;
		await context.SaveChangesAsync(cancellationToken);
		return perfume.Adapt<PerfumeDto>();
	}
}