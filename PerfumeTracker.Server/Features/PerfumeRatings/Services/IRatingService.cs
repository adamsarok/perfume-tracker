namespace PerfumeTracker.Server.Features.PerfumeRatings.Services;

public interface IRatingService {
	Task<PerfumeRating> AddPerfumeRating(Guid perfumeId, decimal rating, string comment, CancellationToken cancellationToken);
	Task<PerfumeRating> DeletePerfumeRating(Guid ratingId, CancellationToken cancellationToken);
}
