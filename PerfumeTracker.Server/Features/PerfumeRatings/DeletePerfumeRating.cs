using PerfumeTracker.Server.Features.PerfumeRatings.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.PerfumeRatings;

public record DeletePerfumeRatingCommand(Guid PerfumeId, Guid RatingId) : ICommand<PerfumeRatingDownloadDto>;
public class DeletePerfumeRatingEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapDelete("/api/perfumes/{perfumeId}/ratings/{ratingId}", async (ISender sender, Guid perfumeId, Guid ratingId, CancellationToken cancellationToken) => {
			await sender.Send(new DeletePerfumeRatingCommand(perfumeId, ratingId), cancellationToken);
			return Results.NoContent();
		}).WithTags("PerfumeRatings")
			.WithName("DeletePerfumeRating")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class DeletePerfumeRatingHandler(IRatingService ratingService) : ICommandHandler<DeletePerfumeRatingCommand, PerfumeRatingDownloadDto> {
	public async Task<PerfumeRatingDownloadDto> Handle(DeletePerfumeRatingCommand request, CancellationToken cancellationToken) {
		var rating = await ratingService.DeletePerfumeRating(request.RatingId, cancellationToken);
		return rating.Adapt<PerfumeRatingDownloadDto>();
	}
}