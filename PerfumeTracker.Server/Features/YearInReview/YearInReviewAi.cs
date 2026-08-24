using Microsoft.Extensions.Caching.Memory;
using OpenAI.Chat;
using PerfumeTracker.Server.Startup;

namespace PerfumeTracker.Server.Features.YearInReview;

public record YearInReviewAiCandidate(
	Guid PerfumeId,
	string House,
	string PerfumeName,
	string Family,
	int YearWears,
	decimal? Rating,
	IReadOnlyList<YearInReviewAiTag> Tags,
	IReadOnlyList<string> CandidateReasons);

public record YearInReviewAiTag(Guid TagId, string Name, string? Description);

public record YearInReviewAiSelection(
	Guid? CrowdPleaserPerfumeId,
	string? CrowdPleaserReason,
	Guid? WildcardPerfumeId,
	string? WildcardReason,
	Guid? ComfortScentPerfumeId,
	string? ComfortScentReason);

public interface IYearInReviewAi {
	Task<YearInReviewAiSelection?> SelectCategories(
		int year,
		IReadOnlyList<YearInReviewAiCandidate> candidates,
		CancellationToken cancellationToken);
}

public sealed class NullYearInReviewAi : IYearInReviewAi {
	public Task<YearInReviewAiSelection?> SelectCategories(
		int year,
		IReadOnlyList<YearInReviewAiCandidate> candidates,
		CancellationToken cancellationToken) => Task.FromResult<YearInReviewAiSelection?>(null);
}

public sealed class YearInReviewAi(
	ChatClient chatClient,
	IMemoryCache memoryCache,
	ILogger<YearInReviewAi> logger) : IYearInReviewAi {
	private static readonly JsonSerializerOptions SerializerOptions = new() {
		PropertyNameCaseInsensitive = true
	};

	public async Task<YearInReviewAiSelection?> SelectCategories(
		int year,
		IReadOnlyList<YearInReviewAiCandidate> candidates,
		CancellationToken cancellationToken) {
		if (candidates.Count == 0) return null;

		var fingerprint = string.Join('|', candidates.Select(x =>
			$"{x.PerfumeId:N}:{x.YearWears}:{x.Rating}:{string.Join(',', x.Tags.Select(t => t.TagId.ToString("N")))}:{string.Join(',', x.CandidateReasons)}"));
		var cacheKey = $"year-review-ai:v4:{year}:{fingerprint}";
		if (memoryCache.TryGetValue<YearInReviewAiSelection>(cacheKey, out var cached)) return cached;

		try {
			var payload = JsonSerializer.Serialize(candidates.Select(x => new {
				perfumeId = x.PerfumeId,
				x.House,
				x.PerfumeName,
				x.Family,
				x.YearWears,
				x.Rating,
				candidateReasons = x.CandidateReasons,
				tags = x.Tags.Select(t => new { tagId = t.TagId, t.Name, t.Description })
			}));

			List<OpenAI.Chat.ChatMessage> messages = [
				new SystemChatMessage(
					"""
					You are a knowledgeable perfume editor creating a playful year in review.
					Select only from the supplied perfume and tag IDs. Never invent an ID.
					- Crowd Pleaser: the most broadly popular and culturally well-known perfume. Use your general perfume knowledge; yearWears is only a tie-breaker.
					- Wildcard: the most unconventional, daring, obscure, or surprising perfume. Consider both the perfume's reputation and its supplied notes/tags.
					- Comfort Scent: the perfume whose identity, family, or notes feel the most cosy, reassuring, or comforting.
					Keep every reason factual, friendly, and under 120 characters. Do not mention that you are an AI.
					"""),
				new UserChatMessage($"Year: {year}\nCandidates JSON:\n{payload}")
			];

			var options = new ChatCompletionOptions {
				ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
					jsonSchemaFormatName: "year_in_review_categories",
					jsonSchema: BinaryData.FromString("""
					{
					  "type": "object",
					  "properties": {
					    "crowdPleaserPerfumeId": { "type": "string" },
					    "crowdPleaserReason": { "type": "string" },
					    "wildcardPerfumeId": { "type": "string" },
					    "wildcardReason": { "type": "string" },
					    "comfortScentPerfumeId": { "type": "string" },
					    "comfortScentReason": { "type": "string" }
					  },
					  "required": [
					    "crowdPleaserPerfumeId", "crowdPleaserReason",
					    "wildcardPerfumeId", "wildcardReason",
					    "comfortScentPerfumeId", "comfortScentReason"
					  ],
					  "additionalProperties": false
					}
					"""),
					jsonSchemaIsStrict: true)
			};

			var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
			Diagnostics.RecordChatTokenUsage(completion.Value, "year_in_review");
			var text = completion.Value.Content.FirstOrDefault()?.Text;
			if (string.IsNullOrWhiteSpace(text)) return null;

			var result = JsonSerializer.Deserialize<YearInReviewAiSelection>(text, SerializerOptions);
			if (result != null) {
				memoryCache.Set(cacheKey, result, TimeSpan.FromHours(6));
			}
			return result;
		} catch (Exception ex) when (ex is not OperationCanceledException) {
			logger.LogWarning(ex, "Could not generate AI categories for year {Year}", year);
			return null;
		}
	}
}
