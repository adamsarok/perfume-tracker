using OpenAI.Chat;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public class PerfumeIdentifier(ChatClient chatClient) : IPerfumeIdentifier {
	public async Task<IdentifiedPerfume> GetIdentifiedPerfumeAsync(string house, string perfumeName, CancellationToken cancellationToken) {
		var messages = new List<ChatMessage> {
			new SystemChatMessage(
				"""
				You are a perfume expert. Given a perfume house and name, identify the perfume's family and notes.
				
				IMPORTANT: 
				- If you're not sure or the perfume doesn't exist, set confidenceScore to 0.0.
				- Base your confidence on your knowledge of real, commercially available perfumes.
				- Confidence scale: 0.0 (not confident/doesn't exist) to 1.0 (very confident/well-known perfume).
				"""
			),
			new UserChatMessage(
				$"Identify the perfume: House: {house}, Name: {perfumeName}"
			)
		};

		var chatCompletionOptions = new ChatCompletionOptions {
			ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
				jsonSchemaFormatName: "identified_perfume",
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
						"family": {
							"type": "string",
							"description": "The perfume family (e.g., Fougere, Floral, Woody, Oriental, Fresh, Leather)"
						},
						"notes": {
							"type": "array",
							"items": {
								"type": "string"
							},
							"description": "List of main perfume notes"
						},
						"confidenceScore": {
							"type": "number",
							"description": "Confidence that this perfume exists and the information is accurate (0.0 to 1.0)"
						}
					},
					"required": ["house", "perfumeName", "family", "notes", "confidenceScore"],
					"additionalProperties": false
				}
				"""),
				jsonSchemaIsStrict: true
			)
		};

		var completion = await chatClient.CompleteChatAsync(messages, chatCompletionOptions, cancellationToken);

		var result = System.Text.Json.JsonSerializer.Deserialize<IdentifiedPerfume>(
			completion.Value.Content[0].Text,
			new System.Text.Json.JsonSerializerOptions {
				PropertyNameCaseInsensitive = true
			}
		);


		return result ?? throw new InvalidOperationException("Failed to parse OpenAI response");
	}
}
