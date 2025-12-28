using PerfumeTracker.Server.Features.Perfumes.Extensions;
using PerfumeTracker.Server.Services.Common;
using PerfumeTracker.Server.Services.Embedding;
using Pgvector.EntityFrameworkCore;
using static PerfumeTracker.Server.Features.Perfumes.GetPerfumeRecommendations;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public enum RecommendationStrategy {
	ForgottenFavorite,  // High rating, not used recently
	SimilarToLastUsed,  // Similar to last 5 used
	Seasonal,           // Based on current season
	Random,             // Random from unused
	LeastUsed,          // Bottom 10% usage
	MoodOrOccasion      // Based on Mood/Occasion input
}

public class PerfumeRecommender(PerfumeTrackerContext context, IEncoder encoder, IPresignedUrlService presignedUrlService) : IPerfumeRecommender {
	private const int RANDOM_SAMPLE_MULTIPLIER = 3;
	private List<Guid>? _lastWornPerfumeIds;
	private async Task<List<Guid>> GetLastWornPerfumeIdsCached(CancellationToken cancellationToken = default) {
		if (_lastWornPerfumeIds == null) {
			_lastWornPerfumeIds = await context
				.PerfumeEvents
				.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn)
				.OrderByDescending(x => x.EventDate)
				.Select(x => x.PerfumeId)
				.Distinct()
				.Take(5)
				.ToListAsync(cancellationToken);
		}
		return _lastWornPerfumeIds;
	}

	private IQueryable<Perfume> GetRecommendablePerfumes(decimal minimumRating, List<Guid> lastWornPerfumeIds) {
		return context.Perfumes
			.Where(p => p.Ml > 0
				&& p.PerfumeRatings.Any()
				&& p.PerfumeRatings.Average(pr => pr.Rating) >= minimumRating
				&& !lastWornPerfumeIds.Contains(p.Id))
			.Include(p => p.PerfumeEvents)
			.Include(p => p.PerfumeRatings)
			.Include(p => p.PerfumeTags)
				.ThenInclude(pt => pt.Tag);
	}

	public async Task<IEnumerable<PerfumeRecommendationDto>> GetRecommendationsForStrategy(RecommendationStrategy strategy, int count, CancellationToken cancellationToken) {
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		return strategy switch {
			RecommendationStrategy.ForgottenFavorite => await GetForgottenFavorites(count, settings, cancellationToken),
			RecommendationStrategy.SimilarToLastUsed => await GetSimilarToLastUsed(count, settings, cancellationToken),
			RecommendationStrategy.Seasonal => await GetSeasonal(count, settings, cancellationToken),
			RecommendationStrategy.Random => await GetRandom(count, settings, cancellationToken),
			RecommendationStrategy.LeastUsed => await GetLeastUsed(count, settings, cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(strategy))
		};
	}

	private async Task<IEnumerable<PerfumeRecommendationDto>> GetLeastUsed(int count, UserProfile userProfile, CancellationToken cancellationToken) {
		var worn = await GetLastWornPerfumeIdsCached(cancellationToken);
		var result = await GetRecommendablePerfumes(userProfile.MinimumRating, worn)
			.OrderBy(p => p.PerfumeEvents.Count(pe => pe.Type == PerfumeEvent.PerfumeEventType.Worn))
			.Take(count * RANDOM_SAMPLE_MULTIPLIER)
			.ToListAsync(cancellationToken);
		return result
			.OrderBy(_ => Random.Shared.Next())
			.Take(count)
			.Select(x => new PerfumeRecommendationDto(x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.LeastUsed));
	}

	private async Task<List<Guid>> GetAlreadySuggestedRandomPerfumeIds(int daysFilter, CancellationToken cancellationToken) {
		return await context
			.PerfumeRandoms
			.Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync(cancellationToken);
	}

	private async Task<IEnumerable<PerfumeRecommendationDto>> GetRandom(int count, UserProfile userProfile, CancellationToken cancellationToken) {
		var alreadySuggestedIds = await GetAlreadySuggestedRandomPerfumeIds(userProfile.DayFilter, cancellationToken);
		var all = await GetRecommendablePerfumes(userProfile.MinimumRating, alreadySuggestedIds)
			.ToListAsync(cancellationToken);
		if (all.Count == 0) return Enumerable.Empty<PerfumeRecommendationDto>();
		var filtered = all.Where(x => !alreadySuggestedIds.Contains(x.Id));
		if (!filtered.Any()) filtered = all;
		return filtered
			.OrderBy(_ => Random.Shared.Next())
			.Take(count)
			.Select(x => new PerfumeRecommendationDto(x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.Random));
	}

	private async Task<IEnumerable<PerfumeRecommendationDto>> GetSeasonal(int count, UserProfile userProfile, CancellationToken cancellationToken) {
		// User might not have logged any tags or comments that indicate seasonality
		// Query more from DB and ask LLM to help pick?
		// Hardcode some common seasonal tags?
		var keywords = GetSeasonalKeywords(Season);
		var alreadySuggestedIds = await GetAlreadySuggestedRandomPerfumeIds(userProfile.DayFilter, cancellationToken);
		var seasonalTagged = await GetRecommendablePerfumes(userProfile.MinimumRating, alreadySuggestedIds)
			.Where(x => x.PerfumeTags.Any(pt => keywords.Contains(pt.Tag.TagName.ToLower())))
			.Take(count * RANDOM_SAMPLE_MULTIPLIER)
			.ToListAsync(cancellationToken);

		// Should there be a filter on recently worn?
		return seasonalTagged
			.OrderBy(_ => Random.Shared.Next())
			.Take(count)
			.Select(x => new PerfumeRecommendationDto(x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.Seasonal));
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

	private async Task<IEnumerable<PerfumeRecommendationDto>> GetSimilarToLastUsed(int count, UserProfile userProfile, CancellationToken cancellationToken) {
		var worn = await GetLastWornPerfumeIdsCached(cancellationToken);
		var tags = await context.Perfumes
			.Where(x => worn.Contains(x.Id))
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Select(x => x.PerfumeTags.Select(pt => pt.Tag.TagName))
			.ToListAsync(cancellationToken);
		var flatTags = tags.SelectMany(x => x).Distinct().ToList();
		var embedding = await encoder.GetEmbeddings(string.Join(" ", flatTags), cancellationToken);
		var result = await GetSimilarToEmbedding(count, userProfile, embedding, null, cancellationToken);
		return result.Select(x => new PerfumeRecommendationDto(x, RecommendationStrategy.SimilarToLastUsed));
	}

	private async Task<IEnumerable<PerfumeWithWornStatsDto>> GetSimilarToEmbedding(int count, UserProfile userProfile,
		Pgvector.Vector embedding, Guid? skipPerfumeId = null, CancellationToken cancellationToken = default) {
		var result = await GetRecommendablePerfumes(userProfile.MinimumRating, await GetLastWornPerfumeIdsCached(cancellationToken))
			.Include(x => x.PerfumeDocument)
			.Where(x => x.PerfumeDocument != null && x.PerfumeDocument.Embedding != null
				&& (skipPerfumeId == null || x.Id != skipPerfumeId))
			.OrderBy(x => x.PerfumeDocument!.Embedding!.L2Distance(embedding))
			.Take(count * RANDOM_SAMPLE_MULTIPLIER)
			.ToListAsync(cancellationToken);
		return result
			.OrderBy(_ => Random.Shared.Next())
			.Take(count)
			.Select(x => x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService));
	}

	public async Task<IEnumerable<PerfumeWithWornStatsDto>> GetSimilar(Guid perfumeId, int count, UserProfile userProfile, CancellationToken cancellationToken) {
		var perfume = await context.PerfumeDocuments
			.FirstOrDefaultAsync(x => x.Id == perfumeId, cancellationToken);
		if (perfume == null || perfume.Embedding == null) {
			return Enumerable.Empty<PerfumeWithWornStatsDto>();
		}
		return await GetSimilarToEmbedding(count, userProfile, perfume.Embedding, perfumeId, cancellationToken);
	}

	private async Task<IEnumerable<PerfumeRecommendationDto>> GetForgottenFavorites(int count, UserProfile userProfile, CancellationToken cancellationToken) {
		// TODO: check if orderby causes performance issues, if so denormalize last worn date into Perfume table
		var worn = await GetLastWornPerfumeIdsCached(cancellationToken);
		var result = await GetRecommendablePerfumes(userProfile.MinimumRating, worn)
			.OrderBy(p => p.PerfumeEvents.Where(pe => pe.Type == PerfumeEvent.PerfumeEventType.Worn).Max(e => e.EventDate))
			.Take(count * RANDOM_SAMPLE_MULTIPLIER)
			.ToListAsync(cancellationToken);
		return result
			.OrderBy(_ => Random.Shared.Next())
			.Take(count)
			.Select(x => new PerfumeRecommendationDto(x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.ForgottenFavorite));
	}

	public Task<IEnumerable<PerfumeRecommendationDto>> GetRecommendationsForOccasionMoodPrompt(int count, string? prompt, CancellationToken cancellationToken) {
		throw new NotImplementedException();
	}
}
