using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Features.PerfumeEvents;

namespace PerfumeTracker.Server.Features.PerfumeRatings;
public record DeletePerfumeRatingCommand(Guid PerfumeId, Guid RatingId) : ICommand<PerfumeRatingDownloadDto>;
public class DeletePerfumeRatingEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfumes/{perfumeId}/ratings/{ratingId}", async (ISender sender, Guid perfumeId, Guid ratingId) => {
			var result = await sender.Send(new DeletePerfumeRatingCommand(perfumeId, ratingId));
			return Results.NoContent();
		}).WithTags("PerfumeRatings")
			.WithName("DeletePerfumeRating")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class DeletePerfumeRatingHandler(PerfumeTrackerContext context) : ICommandHandler<DeletePerfumeRatingCommand, PerfumeRatingDownloadDto> {
	public async Task<PerfumeRatingDownloadDto> Handle(DeletePerfumeRatingCommand request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var rating = await context.PerfumeRatings.FindAsync(request.RatingId);
		if (rating == null) throw new NotFoundException();
		if (rating.PerfumeId != request.PerfumeId) throw new BadRequestException("PerfumeRating does not belong to selected Perfume");
		rating.IsDeleted = true;
		await context.SaveChangesAsync();
		return rating.Adapt<PerfumeRatingDownloadDto>();
	}
}