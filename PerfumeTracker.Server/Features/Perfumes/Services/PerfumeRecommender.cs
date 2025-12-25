using Pgvector.EntityFrameworkCore;
using System.Threading;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public enum RecommendationStrategy {
	ForgottenFavorite,  // High rating, not used recently
	MoodBased,          // Similar to last 5 used
	Seasonal,           // Based on current season
	Random,             // Random from unused
	LeastUsed           // Bottom 10% usage
}

public class PerfumeRecommender(PerfumeTrackerContext context) : IPerfumeRecommender {

	public async Task<IEnumerable<Perfume>> GetRecommendationsForStrategy(RecommendationStrategy strategy, int count, CancellationToken cancellationToken) {
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		return strategy switch {
			RecommendationStrategy.ForgottenFavorite => await GetForgottenFavorite(count, settings.MinimumRating, cancellationToken),
			RecommendationStrategy.MoodBased => await GetMoodBased(count, settings.MinimumRating, cancellationToken),
			RecommendationStrategy.Seasonal => await GetSeasonal(count, settings.MinimumRating, cancellationToken),
			RecommendationStrategy.Random => await GetRandom(count, settings.MinimumRating, cancellationToken),
			RecommendationStrategy.LeastUsed => await GetLeastUsed(count, settings.MinimumRating, cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(strategy))
		};
	}

	private async Task<IEnumerable<Perfume>> GetLeastUsed(int count, decimal minimumRating, CancellationToken cancellationToken) {
		throw new NotImplementedException();
	}

	private async Task<IEnumerable<Perfume>> GetRandom(int count, decimal minimumRating, CancellationToken cancellationToken) {
		throw new NotImplementedException();
	}

	private async Task<IEnumerable<Perfume>> GetSeasonal(int count, decimal minimumRating, CancellationToken cancellationToken) {
		throw new NotImplementedException();
	}

	private async Task<IEnumerable<Perfume>> GetMoodBased(int count, decimal minimumRating, CancellationToken cancellationToken) {
		throw new NotImplementedException();
	}

	public async Task<IEnumerable<Perfume>> GetSimilar(Guid perfumeId, int count, CancellationToken cancellationToken) {
		//1. search embeddings (sentiment, notes) - weighted 80%
		var perfume = await context.Perfumes
			.Include(x => x.PerfumeDocument)
			.FirstOrDefaultAsync(x => x.Id == perfumeId, cancellationToken);
		var embedding = perfume?.PerfumeDocument?.Embedding;
		if (embedding == null) {
			return Enumerable.Empty<Perfume>();
		}
		var similar = await context.PerfumeDocuments
			.Include(x => x.Perfume)
			.OrderBy(x => x.Embedding!.L2Distance(embedding))
			.Take(5)
			.Select(x => x.Perfume)
			.ToListAsync();

		//2. search text (same house, similar name) - weighted 20%
		//3. merge results and return top N

		return similar;
	}

	private async Task<IEnumerable<Perfume>> GetForgottenFavorite(int count, decimal minimumRating, CancellationToken cancellationToken) {
		throw new NotImplementedException();
	}

	IQueryable<Perfume> GetRecommendable(decimal minimumRating) {
		return context
			.Perfumes
			.Where(x => x.Ml > 0 && x.PerfumeRatings.Average(x => x.Rating) >= minimumRating);
	}
}
