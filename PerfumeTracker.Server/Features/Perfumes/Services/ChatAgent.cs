using OpenAI.Chat;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.Perfumes.Extensions;
using PerfumeTracker.Server.Features.Users;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public class ChatAgent(
	PerfumeTrackerContext context,
	ChatClient chatClient,
	IPerfumeRecommender perfumeRecommender,
	IUserProfileService userProfileService) : IChatAgent {

	private static readonly ChatTool SearchByOccasionMoodTool = ChatTool.CreateFunctionTool(
		functionName: "search_perfumes_by_occasion_mood_notes",
		functionDescription: "Search for perfume recommendations based on mood, occasion, tags, notes, or comments. Use this when the user asks for perfumes suitable for specific situations, seasons, or characteristics.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {
				"prompt": {
					"type": "string",
					"description": "The mood, occasion, season, or perfume characteristics to search for (e.g., 'summer night', 'formal meeting', 'spicy amber')"
				},
				"count": {
					"type": "integer",
					"description": "Number of recommendations to return (1-10)",
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

	private static readonly ChatTool SearchByNameTool = ChatTool.CreateFunctionTool(
		functionName: "search_perfumes_by_name",
		functionDescription: "Search for perfumes by name or brand/house. Use this when the user asks about specific perfume names or brands.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {
				"query": {
					"type": "string",
					"description": "The perfume name or house/brand to search for"
				}
			},
			"required": ["query"],
			"additionalProperties": false
		}
		""")
	);

	public async Task<ChatAgentResponse> ChatAsync(ChatAgentRequest request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();

		Models.ChatConversation conversation;
		List<OpenAI.Chat.ChatMessage> chatHistory;

		if (request.ConversationId.HasValue) {
			conversation = await context.Set<Models.ChatConversation>()
				.Include(c => c.Messages)
				.FirstOrDefaultAsync(c => c.Id == request.ConversationId.Value, cancellationToken)
				?? throw new NotFoundException($"Conversation {request.ConversationId.Value} not found");
			chatHistory = await GetChatHistory(conversation);
		} else {
			var userStats = await GetUserStats(cancellationToken);
			conversation = new Models.ChatConversation {
				UserId = userId,
				Title = request.UserMessage.Length > 50 ? request.UserMessage[..50] + "..." : request.UserMessage
			};
			context.Add(conversation);
			await context.SaveChangesAsync(cancellationToken);

			var systemPrompt = BuildSystemPrompt(userStats);
			chatHistory = [new SystemChatMessage(systemPrompt)];
		}

		var userMessage = new UserChatMessage(request.UserMessage);
		chatHistory.Add(userMessage);

		await SaveChatMessage(conversation.Id, "user", request.UserMessage, chatHistory.Count - 1, cancellationToken);

		var options = new ChatCompletionOptions();
		options.Tools.Add(SearchByOccasionMoodTool);
		options.Tools.Add(SearchByNameTool);

		IEnumerable<PerfumeWithWornStatsDto>? recommendedPerfumes = null;
		bool requiresAnotherIteration = true;
		int maxIterations = 5;
		int iteration = 0;

		while (requiresAnotherIteration && iteration < maxIterations) {
			iteration++;
			requiresAnotherIteration = false;

			var completion = await chatClient.CompleteChatAsync(chatHistory, options, cancellationToken);

			switch (completion.Value.FinishReason) {
				case ChatFinishReason.Stop:
					var responseMessage = completion.Value.Content[0].Text;
					await SaveChatMessage(conversation.Id, "assistant", responseMessage, chatHistory.Count, cancellationToken);
					return new ChatAgentResponse(conversation.Id, responseMessage, recommendedPerfumes);

				case ChatFinishReason.ToolCalls:
					chatHistory.Add(new AssistantChatMessage(completion.Value));
					await SaveAssistantMessageWithTools(conversation.Id, completion.Value, chatHistory.Count - 1, cancellationToken);

					foreach (var toolCall in completion.Value.ToolCalls) {
						string toolResult;
						try {
							var perfumes = await ExecuteToolCall(toolCall, cancellationToken);
							recommendedPerfumes = perfumes;
							toolResult = JsonSerializer.Serialize(perfumes.Select(p => new {
								id = p.Perfume.Id,
								house = p.Perfume.House,
								perfumeName = p.Perfume.PerfumeName,
								family = p.Perfume.Family,
								rating = p.averageRating,
								wearCount = p.WornTimes,
								mlLeft = p.Perfume.MlLeft
							}));
						} catch (Exception ex) {
							toolResult = $"Error: {ex.Message}";
						}
						chatHistory.Add(new ToolChatMessage(toolCall.Id, toolResult));
						await SaveChatMessage(conversation.Id, "tool", toolResult, chatHistory.Count - 1, cancellationToken, toolCall.Id, toolCall.FunctionName);
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

	private async Task<IEnumerable<PerfumeWithWornStatsDto>> ExecuteToolCall(ChatToolCall toolCall, CancellationToken cancellationToken) {
		var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolCall.FunctionArguments.ToString());
		if (arguments == null) throw new InvalidOperationException("Failed to parse tool arguments");

		switch (toolCall.FunctionName) {
			case "search_perfumes_by_occasion_mood_notes":
				var prompt = arguments["prompt"].GetString() ?? throw new ArgumentException("Missing prompt");
				var count = arguments.TryGetValue("count", out var countElement) ? countElement.GetInt32() : 5;
				var recommendations = await perfumeRecommender.GetRecommendationsForOccasionMoodPrompt(count, prompt, cancellationToken);
				return recommendations.Select(r => r.Perfume);

			case "search_perfumes_by_name":
				var query = arguments["query"].GetString() ?? throw new ArgumentException("Missing query");
				var tsQuery = string.Join(" & ", query!
					.Split(' ', StringSplitOptions.RemoveEmptyEntries)
					.Select(t => $"{t}:*"));

				var userProfile = await userProfileService.GetCurrentUserProfile(cancellationToken);
				return await context
					.Perfumes
					.Include(x => x.PerfumeEvents)
					.Include(x => x.PerfumeTags)
					.ThenInclude(x => x.Tag)
					.Include(x => x.PerfumeRatings)
					.Where(p => p.FullText.Matches(EF.Functions.ToTsQuery(tsQuery)))
					.Select(p => p.ToPerfumeWithWornStatsDto(userProfile, null!))
					.Take(10)
					.AsSplitQuery()
					.AsNoTracking()
					.ToListAsync();

			default:
				throw new InvalidOperationException($"Unknown tool: {toolCall.FunctionName}");
		}
	}

	private async Task<string> GetUserStats(CancellationToken cancellationToken) {
		var handler = new Features.Users.GetCurrentUserStatsHandler(
			context,
			perfumeRecommender,
			null! // XPService not needed for ToLlmString
		);
		var stats = await handler.Handle(new UserStatsQuery(), cancellationToken);
		return stats.ToLlmString();
	}

	private string BuildSystemPrompt(string userStats) {
		return $"""
You are a helpful perfume assistant. You help users explore their perfume collection, make recommendations, and answer questions about fragrances.

You have access to the user's perfume collection statistics:

{userStats}

When the user asks for recommendations or searches, use the available tools:
- search_perfumes_by_occasion_mood: For mood-based, occasion-based, or characteristic-based searches
- search_perfumes_by_name: For searching by perfume name or brand

Be conversational, friendly, and knowledgeable about perfumes. When you provide recommendations, explain why they might be a good fit based on the user's preferences and history.
""";
	}

	private async Task<List<OpenAI.Chat.ChatMessage>> GetChatHistory(Models.ChatConversation conversation) {
		var messages = conversation.Messages
			.OrderBy(m => m.MessageIndex)
			.ToList();

		var chatHistory = new List<OpenAI.Chat.ChatMessage>();
		var userStats = await GetUserStats(default);
		chatHistory.Add(new SystemChatMessage(BuildSystemPrompt(userStats)));

		foreach (var msg in messages) {
			switch (msg.Role) {
				case "user":
					chatHistory.Add(new UserChatMessage(msg.Content));
					break;
				case "assistant":
					if (!string.IsNullOrEmpty(msg.ToolCallId)) {
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

	private async Task SaveChatMessage(Guid conversationId, string role, string content, int index, CancellationToken cancellationToken, string? toolCallId = null, string? toolName = null) {
		var message = new Models.ChatMessage {
			ConversationId = conversationId,
			Role = role,
			Content = content,
			MessageIndex = index,
			ToolCallId = toolCallId,
			ToolName = toolName,
			UserId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException()
		};
		context.Add(message);
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task SaveAssistantMessageWithTools(Guid conversationId, ChatCompletion completion, int index, CancellationToken cancellationToken) {
		var toolCallsJson = JsonSerializer.Serialize(completion.ToolCalls.Select(tc => new {
			id = tc.Id,
			type = tc.Kind.ToString(),
			function = new {
				name = tc.FunctionName,
				arguments = tc.FunctionArguments.ToString()
			}
		}));

		var message = new Models.ChatMessage {
			ConversationId = conversationId,
			Role = "assistant",
			Content = toolCallsJson,
			MessageIndex = index,
			UserId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException()
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
