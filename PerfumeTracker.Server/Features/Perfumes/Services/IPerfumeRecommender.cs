using static PerfumeTracker.Server.Features.Perfumes.GetPerfumeRecommendations;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public interface IPerfumeRecommender {
	Task<IEnumerable<PerfumeRecommendationDto>> GetRecommendationsForOccasionMoodPrompt(int count, string prompt, CancellationToken cancellationToken);
	Task<IEnumerable<PerfumeWithWornStatsDto>> GetSimilar(Guid perfumeId, int count, UserProfile userProfile, CancellationToken cancellationToken);
	Task<IEnumerable<PerfumeRecommendationDto>> GetAllStrategyRecommendations(int count, CancellationToken cancellationToken);
}
