using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.PerfumeRatings;
public record DeletePerfumeRatingCommand(Guid PerfumeId, Guid RatingId) : ICommand<PerfumeRatingDownloadDto>;
public class DeletePerfumeRatingEndpoint : ICarterModule {
	/// <summary>
	/// Registers the HTTP DELETE endpoint at /api/perfumes/{perfumeId}/ratings/{ratingId} to delete a specific perfume rating.
	/// </summary>
	/// <param name="app">The endpoint route builder used to register routes.</param>
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapDelete("/api/perfumes/{perfumeId}/ratings/{ratingId}", async (ISender sender, Guid perfumeId, Guid ratingId, CancellationToken cancellationToken) => {
			await sender.Send(new DeletePerfumeRatingCommand(perfumeId, ratingId), cancellationToken);
			return Results.NoContent();
		}).WithTags("PerfumeRatings")
			.WithName("DeletePerfumeRating")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class DeletePerfumeRatingHandler(PerfumeTrackerContext context) : ICommandHandler<DeletePerfumeRatingCommand, PerfumeRatingDownloadDto> {
	public async Task<PerfumeRatingDownloadDto> Handle(DeletePerfumeRatingCommand request, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var rating = await context.PerfumeRatings.FindAsync([request.RatingId], cancellationToken) ?? throw new NotFoundException("PerfumeRatings", request.RatingId);
		if (rating.PerfumeId != request.PerfumeId) throw new BadRequestException("PerfumeRating does not belong to selected Perfume");
		rating.IsDeleted = true;
		await context.SaveChangesAsync();
		return rating.Adapt<PerfumeRatingDownloadDto>();
	}
}