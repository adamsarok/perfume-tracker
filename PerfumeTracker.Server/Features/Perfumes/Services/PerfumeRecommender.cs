using OpenAI.Chat;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.Embedding;
using PerfumeTracker.Server.Features.Perfumes.Extensions;
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

public class PerfumeRecommender(PerfumeTrackerContext context,
	IUserProfileService userProfileService,
	IEncoder encoder,
	IPresignedUrlService presignedUrlService,
	ChatClient chatClient) : IPerfumeRecommender {
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
			.Where(p => p.MlLeft > 0
				&& p.PerfumeRatings.Any()
				&& p.PerfumeRatings.Average(pr => pr.Rating) >= minimumRating
				&& !lastWornPerfumeIds.Contains(p.Id))
			.Include(p => p.PerfumeEvents)
			.Include(p => p.PerfumeRatings)
			.Include(p => p.PerfumeTags)
				.ThenInclude(pt => pt.Tag);
	}

	private async Task<IEnumerable<PerfumeRecommendationDto>> GetRecommendationsForStrategy(RecommendationStrategy strategy, int count, CancellationToken cancellationToken) {
		var userProfile = await userProfileService.GetCurrentUserProfile(cancellationToken);
		return strategy switch {
			RecommendationStrategy.ForgottenFavorite => await GetForgottenFavorites(count, userProfile, cancellationToken),
			RecommendationStrategy.SimilarToLastUsed => await GetSimilarToLastUsed(count, userProfile, cancellationToken),
			RecommendationStrategy.Seasonal => await GetSeasonal(count, userProfile, cancellationToken),
			RecommendationStrategy.Random => await GetRandom(count, userProfile, cancellationToken),
			RecommendationStrategy.LeastUsed => await GetLeastUsed(count, userProfile, cancellationToken),
			RecommendationStrategy.MoodOrOccasion => throw new ArgumentException("Use GetRecommendationsForOccasionMoodPrompt for MoodOrOccasion strategy"),
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
			.Select(x => new PerfumeRecommendationDto(Guid.Empty, x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.LeastUsed));
	}

	private async Task<List<Guid>> GetAlreadySuggestedRandomPerfumeIds(int daysFilter, CancellationToken cancellationToken) {
		return await context
			.PerfumeRecommendations
			.Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-daysFilter) && x.Strategy == RecommendationStrategy.Random)
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
			.Select(x => new PerfumeRecommendationDto(Guid.Empty, x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.Random));
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
			.Select(x => new PerfumeRecommendationDto(Guid.Empty, x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.Seasonal));
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
		return result.Select(x => new PerfumeRecommendationDto(Guid.Empty, x, RecommendationStrategy.SimilarToLastUsed));
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
			.Select(x => new PerfumeRecommendationDto(Guid.Empty, x.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.ForgottenFavorite));
	}

	private async Task<CachedCompletion> GetMoodOrOccasionCompletion(string moodOrOccasion, CancellationToken cancellationToken) {
		// Check cache first
		var cached = await context.CachedCompletions
			.FirstOrDefaultAsync(cc => cc.Prompt == moodOrOccasion && cc.CompletionType == CachedCompletion.CompletionTypes.MoodOrOccasionRecommendation, cancellationToken);
		if (cached != null) {
			return cached;
		}
		// Not cached, call OpenAI
		var moodSystemPrompt = new SystemChatMessage(
@"You are a perfume recommendation expert. When given a mood, occasion, or context, respond ONLY with a comma-separated list of perfume notes, accords, and families that match. Do not include explanations, greetings, or any other text. You MUST provide between 7-10 items. Focus on specific notes (e.g., bergamot, vanilla, oud) and general families (e.g., woody, floral, oriental, fresh, aquatic, citrus, spicy, gourmand). Examples:
Query: 'summer night' → Response: 'light, citrus, aquatic, jasmine, neroli, marine, fresh, bergamot'
Query: 'cozy winter evening' → Response: 'warm, amber, vanilla, cinnamon, sandalwood, spicy, gourmand, tonka bean'
Query: 'formal business meeting' → Response: 'fresh, clean, citrus, woody, subtle, bergamot, vetiver, musk'");
		List<OpenAI.Chat.ChatMessage> messages = [
			moodSystemPrompt,
			new UserChatMessage(moodOrOccasion)
		];
		ChatCompletion completion = await chatClient.CompleteChatAsync(messages, cancellationToken: cancellationToken);
		if (completion.Content == null || completion.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Content[0].Text)) {
			throw new InvalidOperationException("OpenAI returned an empty response");
		}
		var text = completion.Content[0].Text;
		// Cache the result
		var cachedCompletion = new CachedCompletion {
			Prompt = moodOrOccasion,
			Response = text,
			CompletionType = CachedCompletion.CompletionTypes.MoodOrOccasionRecommendation,
		};
		context.CachedCompletions.Add(cachedCompletion);
		await context.SaveChangesAsync(cancellationToken);
		return cachedCompletion;
	}

	public record PerfumeRecommendationsAddedNotification(int Count, Guid UserId) : IUserNotification;
	public async Task<IEnumerable<PerfumeRecommendationDto>> GetAllStrategyRecommendations(int count, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var validStrategies = Enum.GetValues<RecommendationStrategy>().Where(s => s != RecommendationStrategy.MoodOrOccasion).ToList();
		int cntPerStrategy = (int)Math.Ceiling((double)count / validStrategies.Count);
		var recommendations = new List<PerfumeRecommendationDto>();
		foreach (var strategy in validStrategies) {
			recommendations.AddRange(await GetRecommendationsForStrategy(strategy, cntPerStrategy, cancellationToken));
		}
		var dedup = recommendations
			.GroupBy(x => x.Perfume.Perfume.Id)
			.Select(g => g.First())
			.ToList();
		return await PersistRecommendations(dedup, count);
	}
	public async Task<IEnumerable<PerfumeRecommendationDto>> GetRecommendationsForOccasionMoodPrompt(int count, string moodOrOccasion, CancellationToken cancellationToken) {
		if (string.IsNullOrWhiteSpace(moodOrOccasion)) {
			throw new ArgumentException("Mood or occasion prompt cannot be empty", nameof(moodOrOccasion));
		}
		var normalizedPrompt = moodOrOccasion.Trim().ToLowerInvariant();
		var completion = await GetMoodOrOccasionCompletion(normalizedPrompt, cancellationToken);
		var embedding = await encoder.GetEmbeddings(completion.Response, cancellationToken);
		var userProfile = await userProfileService.GetCurrentUserProfile(cancellationToken);
		var result = await GetSimilarToEmbedding(count, userProfile, embedding, null, cancellationToken);
		var recommendations = result.Select(x => new PerfumeRecommendationDto(Guid.Empty, x, RecommendationStrategy.MoodOrOccasion));
		return await PersistRecommendations(recommendations, count, completion.Id);
	}

	private async Task<IEnumerable<PerfumeRecommendationDto>> PersistRecommendations(IEnumerable<PerfumeRecommendationDto> recommendations, int count, Guid? completionId = null) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		List<PerfumeRecommendationDto> result = new();
		var limited = recommendations
			.OrderBy(_ => Random.Shared.Next())
			.Take(count);
		foreach (var recommendation in limited) {
			var rec = new PerfumeRecommendation {
				PerfumeId = recommendation.Perfume.Perfume.Id,
				Strategy = recommendation.Strategy,
			};
			if (completionId != null) {
				rec.CompletionId = completionId.Value;
			}
			context.PerfumeRecommendations.Add(rec);
			result.Add(recommendation with { RecommendationId = rec.Id });
		}
		context.OutboxMessages.Add(OutboxMessage.From(new PerfumeRecommendationsAddedNotification(count, userId)));
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<IEnumerable<PerfumeRecommendationStats>> GetRecommendationStats(CancellationToken cancellationToken) {
		return await context.PerfumeRecommendations
			.GroupBy(pr => pr.Strategy)
			.Select(g => new PerfumeRecommendationStats(
				Strategy: g.Key,
				TotalRecommendations: g.Count(),
				AcceptedRecommendations: g.Where(x => x.IsAccepted).Count()
			))
			.ToListAsync(cancellationToken);
	}
}

