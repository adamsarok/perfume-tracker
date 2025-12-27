namespace PerfumeTracker.Server.Features.Perfumes.Services;

public interface IPerfumeRecommender {
	Task<IEnumerable<PerfumeWithWornStatsDto>> GetRecommendationsForStrategy(RecommendationStrategy strategy, int count, CancellationToken cancellationToken);
	Task<IEnumerable<PerfumeWithWornStatsDto>> GetSimilar(Guid perfumeId, int count, UserProfile userProfile, CancellationToken cancellationToken);
}
