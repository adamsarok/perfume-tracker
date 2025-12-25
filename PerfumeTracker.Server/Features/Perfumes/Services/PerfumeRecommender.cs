namespace PerfumeTracker.Server.Features.Perfumes.Services;

public enum RecommendationStrategy {
	ForgottenFavorite,  // High rating, not used recently
	MoodBased,          // Similar to last 5 used
	Seasonal,           // Based on current season
	Random,             // Random from unused
	LeastUsed           // Bottom 10% usage
}

public class PerfumeRecommender : IPerfumeRecommender {
	public async Task<IEnumerable<Perfume>> GetRecommendations(RecommendationStrategy strategy, int count) {
		return strategy switch {
			RecommendationStrategy.ForgottenFavorite => await GetForgottenFavorite(),
			RecommendationStrategy.MoodBased => await GetMoodBased(),
			RecommendationStrategy.Seasonal => await GetSeasonal(),
			RecommendationStrategy.Random => await GetRandom(),
			RecommendationStrategy.LeastUsed => await GetLeastUsed(),
			_ => throw new ArgumentOutOfRangeException(nameof(strategy))
		};
	}

	private async Task<IEnumerable<Perfume>> GetLeastUsed() {
		throw new NotImplementedException();
	}

	private async Task<IEnumerable<Perfume>> GetRandom() {
		throw new NotImplementedException();
	}

	private async Task<IEnumerable<Perfume>> GetSeasonal() {
		throw new NotImplementedException();
	}

	private async Task<IEnumerable<Perfume>> GetMoodBased() {
		throw new NotImplementedException();
	}

	private async Task<IEnumerable<Perfume>> GetForgottenFavorite() {
		throw new NotImplementedException();
	}
}
