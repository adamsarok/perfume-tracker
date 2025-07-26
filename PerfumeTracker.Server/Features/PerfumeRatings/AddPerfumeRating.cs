using PerfumeTracker.Server.Features.Outbox;

namespace PerfumeTracker.Server.Features.PerfumeRatings;
public record PerfumeRatingUploadDto(Guid PerfumeId, decimal Rating, string Comment);
public record PerfumeRatingDownloadDto(Guid PerfumeId, decimal Rating, string Comment, DateTime RatingDate);
public record AddPerfumeRatingCommand(PerfumeRatingUploadDto Dto) : ICommand<PerfumeRatingDownloadDto>;
public class AddPerfumeRatingEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfumes/{id}/ratings", async (PerfumeRatingUploadDto dto, ISender sender) => {
			var result = await sender.Send(new AddPerfumeRatingCommand(dto));
			return Results.CreatedAtRoute("GetPerfumeRatings", new { id = dto.PerfumeId }, result);
		}).WithTags("PerfumeRatings")
			.WithName("PostPerfumeRating")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class AddPerfumeRatingHandler(PerfumeTrackerContext context) : ICommandHandler<AddPerfumeRatingCommand, PerfumeRatingDownloadDto> {
	public async Task<PerfumeRatingDownloadDto> Handle(AddPerfumeRatingCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var evt = request.Dto.Adapt<PerfumeRating>();
		var perfume = await context.Perfumes.FindAsync(evt.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", evt.PerfumeId);
		context.PerfumeRatings.Add(evt);
		var result = evt.Adapt<PerfumeRatingDownloadDto>();
		await context.SaveChangesAsync();
		return result;
	}
}