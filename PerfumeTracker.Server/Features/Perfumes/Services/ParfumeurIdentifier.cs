using Microsoft.Extensions.Caching.Memory;
using OpenAI.Chat;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public class ParfumeurIdentifier(ChatClient chatClient, IMemoryCache memoryCache) : IParfumeurIdentifier {
	private static readonly TimeSpan CompletionCacheTtl = TimeSpan.FromHours(2);

	public async Task<IdentifiedParfumeur> GetIdentifiedParfumeurAsync(string house, string perfumeName, Guid userId, CancellationToken cancellationToken) {
		var promptKey = $"{house}::{perfumeName}";
		var cacheKey = $"completion:identify-parfumeur:{userId}:{promptKey}";
		if (!memoryCache.TryGetValue<string>(cacheKey, out var completionText) || string.IsNullOrWhiteSpace(completionText)) {
			completionText = await GetCompletionTextAsync(house, perfumeName, cancellationToken);
			memoryCache.Set(cacheKey, completionText, new MemoryCacheEntryOptions {
				AbsoluteExpirationRelativeToNow = CompletionCacheTtl
			});
		}

		var result = JsonSerializer.Deserialize<IdentifiedParfumeur>(
			completionText,
			new JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			}
		);

		return result ?? throw new InvalidOperationException("Failed to parse OpenAI response");
	}

	private async Task<string> GetCompletionTextAsync(string house, string perfumeName, CancellationToken cancellationToken) {
		var messages = new List<OpenAI.Chat.ChatMessage> {
			new SystemChatMessage(
				"""
				You are a perfume expert. Given a perfume house and name, identify the parfumeur or perfumer credited with creating the fragrance.

				IMPORTANT:
				- If you are not sure or the perfume does not exist, set confidenceScore to 0.0 and parfumeur to an empty string.
				- Base your confidence on real, commercially available perfumes and commonly cited perfumer credits.
				- If multiple perfumers are clearly credited and you are confident, return a short comma-separated list.
				- Confidence scale: 0.0 (not confident/doesn't exist) to 1.0 (very confident/well-known perfume).
				"""
			),
			new UserChatMessage(
				$"Identify the parfumeur for this perfume: House: {house}, Name: {perfumeName}"
			)
		};

		var chatCompletionOptions = new ChatCompletionOptions {
			ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
				jsonSchemaFormatName: "identified_parfumeur",
				jsonSchema: BinaryData.FromString("""
				{
					"type": "object",
					"properties": {
						"house": {
							"type": "string",
							"description": "The perfume house name"
						},
						"perfumeName": {
							"type": "string",
							"description": "The perfume name"
						},
						"parfumeur": {
							"type": "string",
							"description": "The credited parfumeur or perfumer"
						},
						"confidenceScore": {
							"type": "number",
							"description": "Confidence that the parfumeur information is accurate (0.0 to 1.0)"
						}
					},
					"required": ["house", "perfumeName", "parfumeur", "confidenceScore"],
					"additionalProperties": false
				}
				"""),
				jsonSchemaIsStrict: true
			)
		};

		var completion = await chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);
		PerfumeTracker.Server.Startup.Diagnostics.RecordChatTokenUsage(completion.Value, "identify_parfumeur");
		var content = completion.Value.Content;
		if (content is null || content.Count == 0 || string.IsNullOrEmpty(content[0].Text)) {
			throw new InvalidOperationException("OpenAI returned an empty or invalid response");
		}

		return content[0].Text;
	}
}
