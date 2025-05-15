
namespace PerfumeTracker.Server.Features.Perfumes;
public record DeletePerfumeCommand(int Id) : ICommand;
public class DeletePerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapDelete("/api/perfumes/{id}", async (int id, ISender sender) => {
			var result = await sender.Send(new DeletePerfumeCommand(id));
			return Results.NoContent();
		}).WithTags("Perfumes")
			.WithName("DeletePerfume");
	}
}
public class DeletePerfumeHandler(PerfumeTrackerContext context) : ICommandHandler<DeletePerfumeCommand>  {
	public async Task<Unit> Handle(DeletePerfumeCommand request, CancellationToken cancellationToken) {
		var perfume = await context.Perfumes.FindAsync(request.Id);
		if (perfume == null) throw new NotFoundException();
		context.Perfumes.Remove(perfume);
		await context.SaveChangesAsync();
		return new Unit();
	}
}