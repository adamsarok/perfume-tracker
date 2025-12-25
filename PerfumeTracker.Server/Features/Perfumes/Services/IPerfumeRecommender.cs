namespace PerfumeTracker.Server.Features.Perfumes.Services;

public interface IPerfumeRecommender {
	Task<IEnumerable<Perfume>> GetRecommendationsForStrategy(RecommendationStrategy strategy, int count, CancellationToken cancellationToken);
	Task<IEnumerable<Perfume>> GetSimilar(Guid perfumeId, int count, CancellationToken cancellationToken);
}
