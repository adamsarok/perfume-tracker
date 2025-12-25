namespace PerfumeTracker.Server.Features.Perfumes.Services;

public interface IPerfumeRecommender {
	Task<IEnumerable<Perfume>> GetRecommendations(RecommendationStrategy strategy, int count);
}
