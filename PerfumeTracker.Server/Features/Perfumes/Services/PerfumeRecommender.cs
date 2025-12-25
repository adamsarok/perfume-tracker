using PerfumeTracker.Server.Features.PerfumeRandoms;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server.Services.Embedding;
using Pgvector.EntityFrameworkCore;
using System;
using System.Threading;


namespace PerfumeTracker.Server.Features.Perfumes.Services;

public enum RecommendationStrategy {
	ForgottenFavorite,  // High rating, not used recently
	MoodBased,          // Similar to last 5 used
	Seasonal,           // Based on current season
	Random,             // Random from unused
	LeastUsed           // Bottom 10% usage
}

public class PerfumeRecommender(PerfumeTrackerContext context, IEncoder encoder) : IPerfumeRecommender {
	private const int RANDOM_SAMPLE_MULTIPLIER = 3;
	public async Task<IEnumerable<Perfume>> GetRecommendationsForStrategy(RecommendationStrategy strategy, int count, CancellationToken cancellationToken) {
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		return strategy switch {
			RecommendationStrategy.ForgottenFavorite => await GetForgottenFavorites(count, settings.MinimumRating, cancellationToken),
			RecommendationStrategy.MoodBased => await GetMoodBased(count, settings.MinimumRating, cancellationToken),
			RecommendationStrategy.Seasonal => await GetSeasonal(count, settings.MinimumRating, cancellationToken),
			RecommendationStrategy.Random => await GetRandom(count, settings, cancellationToken),
			RecommendationStrategy.LeastUsed => await GetLeastUsed(count, settings.MinimumRating, cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(strategy))
		};
	}

	private async Task<IEnumerable<Perfume>> GetLeastUsed(int count, decimal minimumRating, CancellationToken cancellationToken) {
		throw new NotImplementedException();
	}
	private async Task<List<Guid>> GetAlreadySuggestedRandomPerfumeIds(int daysFilter, CancellationToken cancellationToken) {
		return await context
			.PerfumeRandoms
			.Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync(cancellationToken);
	}

	private async Task<IEnumerable<Perfume>> GetRandom(int count, UserProfile settings, CancellationToken cancellationToken) {
		var alreadySuggestedIds = await GetAlreadySuggestedRandomPerfumeIds(settings.DayFilter, cancellationToken);
		var worn = await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn && x.EventDate >= DateTimeOffset.UtcNow.AddDays(-settings.DayFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync(cancellationToken);
		var all = await context
			.Perfumes
			.Where(p => p.Ml > 0 && p.PerfumeRatings.Average(pr => pr.Rating) >= settings.MinimumRating)
			.ToListAsync(cancellationToken);
		if (all.Count == 0) return Enumerable.Empty<Perfume>();
		var filtered = all.Where(x => !alreadySuggestedIds.Contains(x.Id) && !worn.Contains(x.Id));
		if (!filtered.Any()) filtered = all.Where(x => !worn.Contains(x.Id));
		if (!filtered.Any()) filtered = all;
		return filtered
			.OrderBy(_ => Random.Shared.Next())
			.Take(count);
	}

	private async Task<IEnumerable<Perfume>> GetSeasonal(int count, decimal minimumRating, CancellationToken cancellationToken) {
		// User might not have logged any tags or comments that indicate seasonality
		// Query more from DB and ask LLM to help pick?
		// Hardcode some common seasonal tags?
		var keywords = GetSeasonalKeywords(Season);
		var seasonalTagged = await context.Perfumes
		.Where(x => x.Ml > 0
			&& x.PerfumeRatings.Average(r => r.Rating) >= minimumRating
			&& x.PerfumeTags.Any(pt => keywords.Contains(pt.Tag.TagName.ToLower())))
		.Include(x => x.PerfumeTags)
		.ThenInclude(x => x.Tag)
		.Take(count * RANDOM_SAMPLE_MULTIPLIER)
		.ToListAsync(cancellationToken);

		// Should there be a filter on recently worn?
		return seasonalTagged
			.OrderBy(_ => Random.Shared.Next())
			.Take(count);
	}
	public enum Seasons { Winter, Spring, Summer, Autumn };
	public static Seasons Season => DateTime.UtcNow.Month switch {
		1 or 2 or 12 => Seasons.Winter,
		3 or 4 or 5 => Seasons.Spring,
		6 or 7 or 8 => Seasons.Summer,
		9 or 10 or 11 => Seasons.Autumn,
		_ => throw new NotImplementedException(),
	};

	private List<string> GetSeasonalKeywords(Seasons season) { // TODO config
		return season switch {
			Seasons.Winter => ["warm", "spicy", "cozy", "amber", "vanilla", "oriental", "gourmand", "oud", "tobacco", "leather"],
			Seasons.Spring => ["fresh", "floral", "green", "light", "airy", "citrus", "jasmine", "rose", "peony", "lilac"],
			Seasons.Summer => ["citrus", "aquatic", "fresh", "light", "fruity", "marine", "coconut", "bergamot", "neroli", "mint"],
			Seasons.Autumn => ["woody", "spicy", "earthy", "amber", "patchouli", "sandalwood", "cinnamon", "nutmeg", "fig", "leather"],
			_ => ["safe", "versatile"]
		};
	}

	private async Task<IEnumerable<Perfume>> GetMoodBased(int count, decimal minimumRating, CancellationToken cancellationToken) {
		var lastFiveIds = await context.PerfumeEvents
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn)
			.OrderByDescending(x => x.EventDate)
			.Select(x => x.PerfumeId)
			.Distinct()
			.Take(5)
			.ToListAsync(cancellationToken);
		var tags = await context.Perfumes
			.Where(x => lastFiveIds.Contains(x.Id))
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Select(x => x.PerfumeTags.Select(pt => pt.Tag.TagName))
			.Distinct()
			.ToListAsync(cancellationToken);
		var flatTags = tags.SelectMany(x => x).Distinct().ToList();
		var embedding = await encoder.GetEmbeddings(string.Join(" ", flatTags), cancellationToken);
		return await GetSimilarToEmbedding(count, minimumRating, lastFiveIds, embedding, cancellationToken);
	}

	private async Task<IEnumerable<Perfume>> GetSimilarToEmbedding(int count, decimal minimumRating, List<Guid> exceptPerfumeIds, Pgvector.Vector embedding, CancellationToken cancellationToken) {
		var result = await context.PerfumeDocuments
					.Where(x => !exceptPerfumeIds.Contains(x.Id)
						&& x.Embedding != null
						&& x.Perfume.Ml > 0
						&& x.Perfume.PerfumeRatings.Average(pr => pr.Rating) >= minimumRating)
					.Include(x => x.Perfume)
					.OrderBy(x => x.Embedding!.L2Distance(embedding))
					.Take(count * RANDOM_SAMPLE_MULTIPLIER)
					.Select(x => x.Perfume)
					.ToListAsync(cancellationToken);
		return result
			.OrderBy(_ => Random.Shared.Next())
			.Take(count);
	}

	public async Task<IEnumerable<Perfume>> GetSimilar(Guid perfumeId, int count, CancellationToken cancellationToken) {
		var userProfile = await context.UserProfiles.FirstAsync(cancellationToken);
		var perfume = await context.Perfumes
			.Include(x => x.PerfumeDocument)
			.FirstOrDefaultAsync(x => x.Id == perfumeId, cancellationToken);
		var embedding = perfume?.PerfumeDocument?.Embedding;
		if (embedding == null) {
			return Enumerable.Empty<Perfume>();
		}
		return await GetSimilarToEmbedding(count, userProfile.MinimumRating, new List<Guid> { perfumeId }, embedding, cancellationToken);
	}

	private async Task<IEnumerable<Perfume>> GetForgottenFavorites(int count, decimal minimumRating, CancellationToken cancellationToken) {
		// TODO: check if orderby causes performance issues, if so denormalize last worn date into Perfume table
		var result = await context.Perfumes
			.Where(p => p.Ml > 0 && p.PerfumeRatings.Average(pr => pr.Rating) >= minimumRating)
			.OrderBy(p => p.PerfumeEvents.Where(pe => pe.Type == PerfumeEvent.PerfumeEventType.Worn).Max(e => e.EventDate))
			.Take(count * RANDOM_SAMPLE_MULTIPLIER)
			.ToListAsync();
		return result
			.OrderBy(_ => Random.Shared.Next())
			.Take(count);
	}

}
