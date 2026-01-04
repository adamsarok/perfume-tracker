using Microsoft.AspNetCore.SignalR;
using OpenAI.Chat;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Users.Services;
using System.Text;

namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public class ChatProgressHub : Hub;
public record PerfumeOwnershipCheckQuery(string House, string Name);
public record PerfumeOwnershipCheckResult(string House, string Name, bool IsOwned);
public record PerfumeLlmDto(string House, string PerfumeName, decimal Rating);
public class ChatAgent(
	PerfumeTrackerContext context,
	ChatClient chatClient,
	IUserStatsService userStatsService,
	ISystemPromptCache promptCache,
	IHubContext<ChatProgressHub> hubContext,
	IChatAgentTools chatAgentTools) : IChatAgent {
	private const int MAX_ITERATIONS = 2;

	public async Task<ChatAgentResponse> ChatAsync(ChatAgentRequest request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();

		var (conversation, chatHistory) = await GetOrCreateConversation(request, userId, cancellationToken);

		var userMessage = new UserChatMessage(request.UserMessage);
		chatHistory.Add(userMessage);

		await SaveChatMessage(conversation.Id, "user", request.UserMessage, chatHistory.Count - 1, cancellationToken, null);

		var options = new ChatCompletionOptions();
		options.Tools.Add(ChatAgentTools.SearchOwnedPerfumesByCharacteristicsTool);
		options.Tools.Add(ChatAgentTools.CheckPerfumeOwnershipTool);

		IEnumerable<PerfumeWithWornStatsDto>? recommendedPerfumes = null;
		bool requiresAnotherIteration = true;
		int iteration = 0;

		while (requiresAnotherIteration && iteration < MAX_ITERATIONS) {
			iteration++;
			requiresAnotherIteration = false;

			// For final iteration disable tool calls to force final answer
			if (iteration == MAX_ITERATIONS) {
				options = new ChatCompletionOptions();
			}

			await hubContext.Clients.User(userId.ToString())
				.SendAsync("ProgressMsg", new { Message = $"Agent is thinking... {iteration}/{MAX_ITERATIONS} iterations." });
			var completion = await chatClient.CompleteChatAsync(chatHistory, options, cancellationToken);

			switch (completion.Value.FinishReason) {
				case ChatFinishReason.Stop:
					var responseMessage = completion.Value.Content[0].Text;
					await SaveChatMessage(conversation.Id, "assistant", responseMessage, chatHistory.Count, cancellationToken, completion.Value.FinishReason);
					return new ChatAgentResponse(conversation.Id, responseMessage, recommendedPerfumes);
				case ChatFinishReason.ToolCalls:
					await HandleToolCalls(userId, conversation, chatHistory, completion, cancellationToken);
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

	private async Task HandleToolCalls(Guid userId, ChatConversation conversation, List<OpenAI.Chat.ChatMessage> chatHistory, System.ClientModel.ClientResult<ChatCompletion> completion, CancellationToken cancellationToken) {
		await hubContext.Clients.User(userId.ToString())
								.SendAsync("ProgressMsg", new { Message = $"Agent is making {completion.Value.ToolCalls.Count} tool call(s)." });
		chatHistory.Add(new AssistantChatMessage(completion.Value));
		await SaveAssistantMessageWithTools(conversation.Id, completion.Value, chatHistory.Count - 1, cancellationToken);

		foreach (var toolCall in completion.Value.ToolCalls) {
			string toolResult;
			try {
				toolResult = await chatAgentTools.ExecuteToolCall(toolCall, cancellationToken);
			} catch (Exception ex) {
				toolResult = $"Error: {ex.Message}";
			}
			chatHistory.Add(new ToolChatMessage(toolCall.Id, toolResult));
			await SaveChatMessage(conversation.Id, "tool", toolResult, chatHistory.Count - 1, cancellationToken, null, toolCall.Id, toolCall.FunctionName);
		}
	}

	private async Task<(ChatConversation conversation, List<OpenAI.Chat.ChatMessage> chatHistory)> GetOrCreateConversation(ChatAgentRequest request, Guid userId, CancellationToken cancellationToken) {
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
		return (conversation, chatHistory);
	}

	private async Task<string?> GetSystemPrompt(Guid userId, CancellationToken cancellationToken) {
		return await promptCache.GetOrBuildSystemPromptAsync(userId, async () => {
			return await BuildSystemPrompt(cancellationToken);
		});
	}

	private async Task<string> BuildSystemPrompt(CancellationToken cancellationToken) {
		var stats = await userStatsService.GetUserStats(cancellationToken);
		StringBuilder sb = new StringBuilder();

		sb.AppendLine($"User's FAVORITE perfume notes:");
		foreach (var note in stats.FavoriteTags) {
			sb.AppendLine($"{note.TagName}");
		}

		sb.AppendLine($"User's FAVORITE perfumes:");
		foreach (var perfume in stats.FavoritePerfumes) {
			sb.AppendLine($"{perfume.House} - {perfume.PerfumeName} rated {perfume.AverageRating:F1})");
		}
		return $"""
You are a helpful perfume assistant. You help users explore their perfume collection, make recommendations, and answer questions about fragrances.

You have access to the user's perfume collection:

{sb.ToString()}

IMPORTANT TOOL USAGE RULES:
1. The available tools ONLY search perfumes the user ALREADY OWNS - they cannot find new perfumes to buy
2. For NEW perfume recommendations (e.g., "recommend new perfumes to try"):
   - Use your perfume knowledge and the user's favorite notes/tags above
   - Suggest perfumes that complement their collection
   - Call check_perfume_ownership with a large batch (30-50 perfumes) in ONE tool call
   - Filter out owned ones and present the remaining recommendations

Available tools (for OWNED perfumes only):
- search_owned_perfumes_by_characteristics: Simple 1-3 word searches in owned collection (e.g., "vanilla", "summer", "woody fresh")
- check_perfume_ownership: Check if user already owns specific perfumes. Use this BEFORE recommending new perfumes to buy.

When tools return perfumes, they include:
- Id, House, PerfumeName, Family
- Rating: User's rating (0-10)
- TimesWorn: How many times worn
- Tags: Notes and characteristics
- LastComment: User's most recent comment

Be conversational, friendly, and knowledgeable. Remember: ONE tool call round only, then provide your final answer.
""";
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
