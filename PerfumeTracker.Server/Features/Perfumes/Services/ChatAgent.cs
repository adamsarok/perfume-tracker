using Microsoft.AspNetCore.SignalR;
using OpenAI.Chat;
using PerfumeTracker.Server.Features.Users;
using System.Text;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public class ChatProgressHub : Hub;
public class ChatAgent(
	PerfumeTrackerContext context,
	ChatClient chatClient,
	IPerfumeRecommender perfumeRecommender,
	ISystemPromptCache promptCache,
	IHubContext<ChatProgressHub> hubContext) : IChatAgent {

	private static readonly ChatTool SearchOwnedPerfumesByCharacteristicsTool = ChatTool.CreateFunctionTool(
		functionName: "search_owned_perfumes_by_characteristics",
		functionDescription: "Search ONLY perfumes the user already OWNS by simple characteristics like notes, moods, or seasons. LIMITATIONS: (1) Only searches user's OWNED collection, NOT new perfumes. (2) Only handles simple 1-3 word queries like 'vanilla', 'summer night', 'spicy amber'.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {
				"prompt": {
					"type": "string",
					"description": "Simple 1-3 word characteristic to search (e.g., 'vanilla', 'summer', 'woody fresh', 'cozy'). Must be a simple note, mood, or season - NOT a complex question."
				},
				"count": {
					"type": "integer",
					"description": "Number of perfumes to return (1-10)",
					"minimum": 1,
					"maximum": 10,
					"default": 5
				}
			},
			"required": ["prompt"],
			"additionalProperties": false
		}
		""")
	);

	private static readonly ChatTool CheckPerfumeOwnershipTool = ChatTool.CreateFunctionTool(
		functionName: "check_perfume_ownership",
		functionDescription: "Check if user already owns specific perfumes by house and name. Use this BEFORE recommending new perfumes to buy. Returns which perfumes from the list are already owned.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {
				"perfumes": {
					"type": "array",
					"description": "List of perfumes to check ownership for",
					"items": {
						"type": "object",
						"properties": {
							"house": {
								"type": "string",
								"description": "Perfume house/brand name"
							},
							"name": {
								"type": "string",
								"description": "Perfume name"
							}
						},
						"required": ["house", "name"]
					}
				}
			},
			"required": ["perfumes"],
			"additionalProperties": false
		}
		""")
	);
	private record PerfumeOwnershipCheckQuery(string House, string Name);

	private record PerfumeOwnershipCheckResult(string House, string Name, bool IsOwned);

	public async Task<ChatAgentResponse> ChatAsync(ChatAgentRequest request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();

		Models.ChatConversation conversation;
		List<OpenAI.Chat.ChatMessage> chatHistory;

		if (request.ConversationId.HasValue) {
			conversation = await context.Set<Models.ChatConversation>()
				.Include(c => c.Messages)
				.FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, cancellationToken)
				?? throw new NotFoundException($"Conversation {request.ConversationId.Value} not found");
			chatHistory = await GetChatHistory(conversation, cancellationToken);
		} else {
			conversation = new Models.ChatConversation {
				UserId = userId,
				Title = request.UserMessage.Length > 50 ? request.UserMessage[..50] + "..." : request.UserMessage
			};
			context.Add(conversation);
			await context.SaveChangesAsync(cancellationToken);

			string? systemPrompt = await GetSystemPrompt(userId, cancellationToken);

			chatHistory = [new SystemChatMessage(systemPrompt)];
		}

		var userMessage = new UserChatMessage(request.UserMessage);
		chatHistory.Add(userMessage);

		await SaveChatMessage(conversation.Id, "user", request.UserMessage, chatHistory.Count - 1, cancellationToken, null);

		var options = new ChatCompletionOptions();
		options.Tools.Add(SearchOwnedPerfumesByCharacteristicsTool);
		options.Tools.Add(CheckPerfumeOwnershipTool);

		IEnumerable<PerfumeWithWornStatsDto>? recommendedPerfumes = null;
		bool requiresAnotherIteration = true;
		int maxIterations = 5;
		int iteration = 0;

		while (requiresAnotherIteration && iteration < maxIterations) {
			iteration++;
			requiresAnotherIteration = false;

			await hubContext.Clients.User(userId.ToString())
				.SendAsync("ProgressMsg", new { Message = $"Agent is thinking... {iteration}/{maxIterations} iterations." });
			var completion = await chatClient.CompleteChatAsync(chatHistory, options, cancellationToken);

			switch (completion.Value.FinishReason) {
				case ChatFinishReason.Stop:
					var responseMessage = completion.Value.Content[0].Text;
					await SaveChatMessage(conversation.Id, "assistant", responseMessage, chatHistory.Count, cancellationToken, completion.Value.FinishReason);
					return new ChatAgentResponse(conversation.Id, responseMessage, recommendedPerfumes);

				case ChatFinishReason.ToolCalls:
					await hubContext.Clients.User(userId.ToString())
						.SendAsync("ProgressMsg", new { Message = $"Agent is making {completion.Value.ToolCalls.Count} tool call(s)." });
					chatHistory.Add(new AssistantChatMessage(completion.Value));
					await SaveAssistantMessageWithTools(conversation.Id, completion.Value, chatHistory.Count - 1, cancellationToken);

					foreach (var toolCall in completion.Value.ToolCalls) {
						string toolResult;
						try {
							toolResult = await ExecuteToolCall(toolCall, cancellationToken);
						} catch (Exception ex) {
							toolResult = $"Error: {ex.Message}";
						}
						chatHistory.Add(new ToolChatMessage(toolCall.Id, toolResult));
						await SaveChatMessage(conversation.Id, "tool", toolResult, chatHistory.Count - 1, cancellationToken, null, toolCall.Id, toolCall.FunctionName);
					}

					requiresAnotherIteration = true;
					break;

				case ChatFinishReason.Length:
					throw new InvalidOperationException("Chat response was too long");

				default:
					throw new InvalidOperationException($"Unexpected finish reason: {completion.Value.FinishReason}");
			}
		}

		throw new InvalidOperationException("Maximum iterations reached without completion");
	}

	private async Task<string?> GetSystemPrompt(Guid userId, CancellationToken cancellationToken) {
		return await promptCache.GetOrBuildSystemPromptAsync(userId, async () => {
			return await BuildSystemPrompt(cancellationToken);
		});
	}

	public record PerfumeLlmDto(
		string House,
		string PerfumeName,
		decimal Rating
	);


	private async Task<string> ExecuteToolCall(ChatToolCall toolCall, CancellationToken cancellationToken) {
		var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolCall.FunctionArguments.ToString());
		if (arguments == null) throw new InvalidOperationException("Failed to parse tool arguments");

		switch (toolCall.FunctionName) {
			case "search_owned_perfumes_by_characteristics":
				var prompt = arguments["prompt"].GetString() ?? throw new ArgumentException("Missing prompt");
				var count = arguments.TryGetValue("count", out var countElement) ? countElement.GetInt32() : 5;
				var recommendations = await perfumeRecommender.GetRecommendationsForOccasionMoodPrompt(count, prompt, cancellationToken);
				var perfumes = recommendations.Select(r => new PerfumeLlmDto(r.Perfume.Perfume.House, r.Perfume.Perfume.PerfumeName, r.Perfume.AverageRating));
				return JsonSerializer.Serialize(perfumes, new JsonSerializerOptions {
					WriteIndented = false
				});
			case "check_perfume_ownership":
				var perfumesElement = arguments["perfumes"];
				var perfumesToCheck = perfumesElement.Deserialize<List<PerfumeOwnershipCheckQuery>>(new JsonSerializerOptions {
					PropertyNameCaseInsensitive = true
				});
				if (perfumesToCheck == null) throw new ArgumentException("Missing perfumes");


				var ownedPerfumes = await context.Perfumes
					.AsNoTracking()
					.Select(r => new PerfumeLlmDto(r.House, r.PerfumeName, r.AverageRating))
					.ToListAsync(cancellationToken);

				var results = perfumesToCheck.Select<PerfumeOwnershipCheckQuery, PerfumeOwnershipCheckResult>(check => {
					var searchText = $"{check.House} {check.Name}".ToLowerInvariant();

					var exactMatch = ownedPerfumes.FirstOrDefault(owned =>
						owned.House.Equals(check.House, StringComparison.OrdinalIgnoreCase) &&
						owned.PerfumeName.Equals(check.Name, StringComparison.OrdinalIgnoreCase));

					if (exactMatch != null) return new PerfumeOwnershipCheckResult(check.House, check.Name, true);

					var fuzzyMatch = ownedPerfumes
						.Select(owned => new {
							owned.House,
							owned.PerfumeName,
							owned.Rating,
							houseScore = CalculateSimilarity(check.House.ToLowerInvariant(), owned.House.ToLowerInvariant()),
							nameScore = CalculateSimilarity(check.Name.ToLowerInvariant(), owned.PerfumeName.ToLowerInvariant())
						})
						.Where(m => (m.houseScore > 0.7 && m.nameScore > 0.7))
						.OrderByDescending(m => m.houseScore + m.nameScore)
						.FirstOrDefault();

					if (fuzzyMatch != null) return new PerfumeOwnershipCheckResult(check.House, check.Name, true);

					return new PerfumeOwnershipCheckResult(check.House, check.Name, false);
				}).ToList();

				return JsonSerializer.Serialize(results, new JsonSerializerOptions {
					WriteIndented = false
				});
			default:
				throw new InvalidOperationException($"Unknown tool: {toolCall.FunctionName}");
		}
	}

	private async Task<UserStatsResponse> GetUserStats(CancellationToken cancellationToken) {
		var handler = new Features.Users.GetCurrentUserStatsHandler(
			context,
			perfumeRecommender,
			null! // XPService not needed for ToLlmString
		);
		return await handler.Handle(new UserStatsQuery(), cancellationToken);
	}

	private async Task<string> BuildSystemPrompt(CancellationToken cancellationToken) {
		var faves = await context
			.Perfumes
			.AsNoTracking()
			.OrderByDescending(p => p.AverageRating)
			.Take(20)
			.Select(x => new {
				x.Id,
				x.House,
				x.PerfumeName,
				x.AverageRating
			})
			.ToListAsync(cancellationToken);

		var stats = await GetUserStats(cancellationToken);
		StringBuilder sb = new StringBuilder();

		sb.AppendLine($"User's FAVORITE perfume notes:");
		foreach (var note in stats.FavoriteTags) {
			sb.AppendLine($"{note.TagName}");
		}

		sb.AppendLine($"User's FAVORITE perfumes:");
		foreach (var perfume in faves) {
			sb.AppendLine($"{perfume.House} - {perfume.PerfumeName} rated {perfume.AverageRating:F1})");
		}
		return $"""
You are a helpful perfume assistant. You help users explore their perfume collection, make recommendations, and answer questions about fragrances.

You have access to the user's perfume collection:

{sb.ToString()}

IMPORTANT TOOL USAGE RULES:
1. The available tools ONLY search perfumes the user ALREADY OWNS - they cannot find new perfumes to buy
2. For NEW perfume recommendations (e.g., "recommend new perfumes to try"):
   - User's favorite notes/tags from their collection
   - Their highest-rated perfume families
   - Perfumes that complement what they already love
   - Make sure to suggest perfumes they DON'T already own, based on the collection above
   - If the user asks for 10 recommendations, search 50 perfumes with check_perfume_ownership and filter out owned ones
   - Provide recommendations after ONLY ONE tool call

Available tools (for OWNED perfumes only):
- search_owned_perfumes_by_characteristics: Simple 1-3 word searches in owned collection (e.g., "vanilla", "summer", "woody fresh")
- check_perfume_ownership: Check if user already owns specific perfumes. Use this BEFORE recommending new perfumes to buy.

When tools return perfumes, they include:
- Id, House, PerfumeName, Family
- Rating: User's rating (0-10)
- TimesWorn: How many times worn
- Tags: Notes and characteristics
- LastComment: User's most recent comment

Be conversational, friendly, and knowledgeable. When recommending NEW perfumes to try, use your perfume knowledge to suggest complementary scents based on their preferences, but make sure they're not already in their collection.
""";
	}

	private static double CalculateSimilarity(string source, string target) {
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0;
		if (source == target) return 1.0;

		var sourceLength = source.Length;
		var targetLength = target.Length;
		var distance = LevenshteinDistance(source, target);

		return 1.0 - (double)distance / Math.Max(sourceLength, targetLength);
	}

	private static int LevenshteinDistance(string source, string target) {
		if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
		if (string.IsNullOrEmpty(target)) return source.Length;

		var sourceLength = source.Length;
		var targetLength = target.Length;
		var distance = new int[sourceLength + 1, targetLength + 1];

		for (var i = 0; i <= sourceLength; distance[i, 0] = i++) { }
		for (var j = 0; j <= targetLength; distance[0, j] = j++) { }

		for (var i = 1; i <= sourceLength; i++) {
			for (var j = 1; j <= targetLength; j++) {
				var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
				distance[i, j] = Math.Min(
					Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
					distance[i - 1, j - 1] + cost);
			}
		}

		return distance[sourceLength, targetLength];
	}

	private async Task<List<OpenAI.Chat.ChatMessage>> GetChatHistory(Models.ChatConversation conversation, CancellationToken cancellationToken) {
		var messages = conversation.Messages
			.OrderBy(m => m.MessageIndex)
			.ToList();

		var chatHistory = new List<OpenAI.Chat.ChatMessage>();
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		string? systemPrompt = await GetSystemPrompt(userId, cancellationToken);
		chatHistory.Add(new SystemChatMessage(systemPrompt));

		foreach (var msg in messages) {
			switch (msg.Role) {
				case "user":
					chatHistory.Add(new UserChatMessage(msg.Content));
					break;
				case "assistant":
					if (msg.ChatFinishReason == ChatFinishReason.ToolCalls) {
						// Deserialize the tool calls structure we saved
						var toolCallsData = JsonSerializer.Deserialize<JsonElement>(msg.Content);
						if (toolCallsData.ValueKind == JsonValueKind.Array) {
							var toolCalls = new List<ChatToolCall>();
							foreach (var tcElement in toolCallsData.EnumerateArray()) {
								var id = tcElement.GetProperty("Id").GetString();
								var kind = tcElement.GetProperty("Kind");
								var functionName = tcElement.GetProperty("FunctionName").GetString();
								var functionArguments = tcElement.GetProperty("FunctionArguments").GetRawText();

								if (id != null && functionName != null && functionArguments != null) {
									toolCalls.Add(ChatToolCall.CreateFunctionToolCall(id, functionName, BinaryData.FromString(functionArguments)));
								}
							}

							if (toolCalls.Count > 0) {
								chatHistory.Add(new AssistantChatMessage((IEnumerable<ChatToolCall>)toolCalls));
								break;
							}
						}
						// If we can't parse tool calls, fall through to regular message
						chatHistory.Add(new AssistantChatMessage(msg.Content));
					} else {
						chatHistory.Add(new AssistantChatMessage(msg.Content));
					}
					break;
				case "tool":
					if (!string.IsNullOrEmpty(msg.ToolCallId)) {
						chatHistory.Add(new ToolChatMessage(msg.ToolCallId, msg.Content));
					}
					break;
			}
		}

		return chatHistory;
	}

	private async Task SaveChatMessage(Guid conversationId, string role, string content, int index, CancellationToken cancellationToken, ChatFinishReason? chatFinishReason, string? toolCallId = null, string? toolName = null) {
		var message = new Models.ChatMessage {
			ConversationId = conversationId,
			Role = role,
			Content = content,
			MessageIndex = index,
			ToolCallId = toolCallId,
			ToolName = toolName,
			UserId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException(),
			ChatFinishReason = chatFinishReason
		};
		context.Add(message);
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task SaveAssistantMessageWithTools(Guid conversationId, ChatCompletion completion, int index, CancellationToken cancellationToken) {
		// IEnumerable<ChatToolCall> toolCalls
		var toolCallsJson = JsonSerializer.Serialize(completion.ToolCalls);

		var message = new Models.ChatMessage {
			ConversationId = conversationId,
			Role = "assistant",
			Content = toolCallsJson,
			MessageIndex = index,
			UserId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException(),
			ChatFinishReason = ChatFinishReason.ToolCalls
		};
		context.Add(message);
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task<Models.ChatConversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken) {
		return await context.Set<Models.ChatConversation>()
			.Include(c => c.Messages)
			.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);
	}

	public async Task<IEnumerable<Models.ChatConversation>> GetUserConversationsAsync(CancellationToken cancellationToken) {
		return await context.Set<Models.ChatConversation>()
			.OrderByDescending(c => c.UpdatedAt)
			.Take(50)
			.ToListAsync(cancellationToken);
	}
}
