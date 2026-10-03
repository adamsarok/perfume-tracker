#pragma warning disable OPENAI001 // Responses API is marked experimental in OpenAI 2.13.0.
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Responses;
using PerfumeTracker.Server.Options;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Users.Services;
using PerfumeTracker.Server.Startup;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public class ChatProgressHub : Hub;
public record PerfumeOwnershipCheckQuery(string House, string Name);
public record PerfumeOwnershipCheckResult(string House, string Name, bool IsOwned);
public record PerfumeLlmDto(
	Guid Id,
	string House,
	string PerfumeName,
	string Family,
	decimal Rating,
	int TimesWorn,
	DateTime? LastWorn,
	decimal MlLeft,
	List<string> Tags,
	string? LastComment);
public class ChatAgent(
	PerfumeTrackerContext context,
	ResponsesClient responsesClient,
	IOptions<OpenAIOptions> openAiOptions,
	IUserStatsService userStatsService,
	ISystemPromptCache promptCache,
	IHubContext<ChatProgressHub> hubContext,
	IChatAgentTools chatAgentTools,
	IOptions<ChatAgentOptions> chatAgentOptions,
	ILogger<ChatAgent> logger) : IChatAgent {
	private static readonly Regex OwnedPerfumeLinkRegex = new(
		@"/perfumes/(?<id>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})",
		RegexOptions.Compiled);

	public async Task<ChatAgentResponse> ChatAsync(ChatAgentRequest request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var maxIterations = Math.Max(1, chatAgentOptions.Value.MaxIterations);

		var (conversation, chatHistory) = await GetOrCreateConversation(request, userId, cancellationToken);

		var userMessage = new UserChatMessage(request.UserMessage);
		chatHistory.Add(userMessage);

		await SaveChatMessage(conversation.Id, "user", request.UserMessage, chatHistory.Count - 1, cancellationToken, null);

		var responseHistory = ChatAgentResponseProtocol.ConvertHistory(chatHistory);
		for (var iteration = 1; iteration <= maxIterations; iteration++) {
			var options = ChatAgentResponseProtocol.CreateOptions(openAiOptions.Value.AssistantModel,
				responseHistory, chatAgentTools.Tools, allowTools: iteration < maxIterations);

			await hubContext.Clients.User(userId.ToString())
				.SendAsync("ProgressMsg", new { Message = $"Agent is thinking... {iteration}/{maxIterations} iterations." }, cancellationToken);
			var response = (await responsesClient.CreateResponseAsync(options, cancellationToken)).Value;
			Diagnostics.RecordChatTokenUsage(response, "chat_agent");
			if (response.Status != ResponseStatus.Completed) {
				throw new InvalidOperationException($"Chat response did not complete: {response.Status}. {response.Error?.Message} {response.IncompleteStatusDetails?.Reason}");
			}

			// Reasoning items must accompany the function calls when sending their outputs back.
			responseHistory.AddRange(response.OutputItems);
			var toolCalls = ChatAgentResponseProtocol.GetToolCalls(response);
			if (toolCalls.Count > 0) {
				if (iteration == maxIterations) throw new InvalidOperationException("Model called tools when tool calls were disabled");
				await HandleToolCalls(userId, conversation, chatHistory, responseHistory, toolCalls, cancellationToken);
				continue;
			}

			var responseMessage = response.GetOutputText();
			if (string.IsNullOrWhiteSpace(responseMessage)) throw new InvalidOperationException("Chat response contained no answer");
			await SaveChatMessage(conversation.Id, "assistant", responseMessage, chatHistory.Count, cancellationToken, ChatFinishReason.Stop);
			await SaveDiscussedPerfumeIds(conversation, responseMessage, cancellationToken);
			await GenerateAndSaveConversationTitle(conversation, cancellationToken);
			return new ChatAgentResponse(conversation.Id, responseMessage);
		}
		throw new InvalidOperationException("Maximum iterations reached without completion");
	}

	private async Task HandleToolCalls(Guid userId, ChatConversation conversation, List<OpenAI.Chat.ChatMessage> chatHistory, List<ResponseItem> responseHistory, List<ChatToolCall> toolCalls, CancellationToken cancellationToken) {
		await hubContext.Clients.User(userId.ToString())
								.SendAsync("ProgressMsg", new { Message = $"Agent is making {toolCalls.Count} tool call(s)." }, cancellationToken);
		chatHistory.Add(new AssistantChatMessage(toolCalls));
		await SaveAssistantMessageWithTools(conversation.Id, toolCalls, chatHistory.Count - 1, cancellationToken);

		foreach (var toolCall in toolCalls) {
			string toolResult;
			var stopwatch = Stopwatch.StartNew();
			var functionArguments = toolCall.FunctionArguments.ToString();

			try {
				toolResult = await chatAgentTools.ExecuteToolCall(toolCall, cancellationToken);
			} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
				throw;
			} catch (Exception ex) {
				stopwatch.Stop();
				logger.LogError(
					ex,
					"Chat agent tool call failed. ConversationId: {ConversationId}, UserId: {UserId}, ToolCallId: {ToolCallId}, ToolName: {ToolName}, ElapsedMs: {ElapsedMs}, Arguments: {Arguments}",
					conversation.Id,
					userId,
					toolCall.Id,
					toolCall.FunctionName,
					stopwatch.ElapsedMilliseconds,
					functionArguments);
				toolResult = $"Error: {ex.Message}";
			}
			responseHistory.Add(ResponseItem.CreateFunctionCallOutputItem(toolCall.Id, toolResult));
			chatHistory.Add(new ToolChatMessage(toolCall.Id, toolResult));
			await SaveChatMessage(conversation.Id, "tool", toolResult, chatHistory.Count - 1, cancellationToken, null, toolCall.Id, toolCall.FunctionName, functionArguments);
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
		var maxResultsPerToolCall = Math.Max(1, chatAgentOptions.Value.MaxResultsPerToolCall);
		StringBuilder sb = new StringBuilder();

		sb.AppendLine($"User's FAVORITE perfume notes:");
		foreach (var note in stats.FavoriteTags) {
			sb.AppendLine($"{note.TagName}");
		}

		sb.AppendLine($"User's FAVORITE perfumes:");
		foreach (var perfume in stats.FavoritePerfumes) {
			sb.AppendLine($"{perfume.House} - {perfume.PerfumeName} rated {perfume.LatestRating:F1})");
		}

		var hasMarketplaceOffers = chatAgentTools.HasMarketplaceOffers;
		var marketplaceToolSelection = hasMarketplaceOffers
			? "- Use search_marketplace_offers for buy-list, decant, sample-shopping, seller, marketplace, requests. This searches locally imported offers only."
			: "";
		var marketplaceToolDescription = hasMarketplaceOffers
			? "- search_marketplace_offers: Search local marketplace offers."
			: "";

		return $"""
You are a helpful perfume assistant. You help users explore their perfume collection, make recommendations, and answer questions about fragrances.

You have access to the user's perfume collection:

{sb.ToString()}

IMPORTANT TOOL USAGE RULES:
1. The available tools ONLY search perfumes the user ALREADY OWNS - they cannot find new perfumes to buy
2. For NEW perfume recommendations (e.g., "recommend new perfumes to try"):
   - Use your perfume knowledge and the user's favorite notes/tags above
   - Suggest perfumes that complement their collection
   - Call check_perfume_ownership with a large batch of up to {maxResultsPerToolCall} perfumes in ONE tool call
   - Filter out owned ones and present the remaining recommendations
   - Do not call search_owned_perfumes_by_characteristics or filter_owned_perfumes for new perfume recommendations unless the user also asks about owned perfumes

Tool selection:
{marketplaceToolSelection}
- Use check_perfume_ownership for general new perfume recommendations, wishlists, and any question where you need to avoid recommending already-owned perfumes.
- Use analyze_wardrobe_gaps for collection gaps, missing scent categories, balance, overrepresented/underrepresented notes, and what note groups to explore next.
- Use filter_owned_perfumes when the user asks for factual lists from their owned collection: highest/lowest rated, most/least worn, not worn recently, never worn, available bottles, house/family/tag filters, or sorted collection views.
- Use search_owned_perfumes_by_characteristics only for fuzzy owned-collection searches by simple notes, moods, seasons, or characteristics.
- When a tool has a count parameter and the user does not ask for a smaller list, request {maxResultsPerToolCall} results so you have enough evidence before narrowing the final answer.

Available tools:
- search_owned_perfumes_by_characteristics: Simple 1-3 word searches in owned collection (e.g., "vanilla", "summer", "woody fresh")
- filter_owned_perfumes: Deterministically filter and order owned perfumes by rating, wear count, last worn date, house, family, tags, and availability.
- check_perfume_ownership: Check if user already owns specific perfumes. Use this BEFORE recommending new perfumes to buy.
- analyze_wardrobe_gaps: Deterministically analyzes the user's owned collection by NoteGroup and returns missing, thin, balanced, and strong note groups.
{marketplaceToolDescription}

For wardrobe-gap requests:
- Always call analyze_wardrobe_gaps.
- Explain that this is note-group-level coverage, not a full style/performance/occasion audit.
- Highlight missing and thin groups first, then mention strong areas for context.
- Suggest exploration directions, not mandatory purchases.
- If the tool reports many ungrouped perfumes, mention that better NoteGroup backfill will improve the analysis.

When tools return perfumes, they include:
- Id, House, PerfumeName, Family
- Rating: User's rating (0-10)
- TimesWorn: How many times worn
- LastWorn and MlLeft
- Tags: Notes and characteristics
- LastComment: User's most recent comment

OWNED PERFUME REFERENCES:
- Whenever the final answer mentions a perfume the user owns, format its name as a markdown link using its returned Id: [House - PerfumeName](/perfumes/00000000-0000-0000-0000-000000000000)
- Use the exact owned perfume Id returned by a tool. Never invent an Id.
- Do not use this link format for perfumes the user does not own.

Use the tools to gather enough collection evidence before answering, especially for personalized wear recommendations. Be conversational, friendly, and knowledgeable.
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
						var toolCalls = ChatAgentResponseProtocol.DeserializeToolCalls(msg.Content);
						if (toolCalls.Count > 0) {
							chatHistory.Add(new AssistantChatMessage(toolCalls));
							break;
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

	private async Task SaveChatMessage(Guid conversationId, string role, string content, int index, CancellationToken cancellationToken, ChatFinishReason? chatFinishReason, string? toolCallId = null, string? toolName = null, string? toolCallArguments = null) {
		var message = new Models.ChatMessage {
			ConversationId = conversationId,
			Role = role,
			Content = content,
			MessageIndex = index,
			ToolCallId = toolCallId,
			ToolName = toolName,
			ToolCallArguments = toolCallArguments,
			UserId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException(),
			ChatFinishReason = chatFinishReason
		};
		context.Add(message);
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task SaveAssistantMessageWithTools(Guid conversationId, List<ChatToolCall> toolCalls, int index, CancellationToken cancellationToken) {
		var toolCallsJson = JsonSerializer.Serialize(toolCalls);

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

	private async Task SaveDiscussedPerfumeIds(ChatConversation conversation, string assistantResponse, CancellationToken cancellationToken) {
		var referencedIds = OwnedPerfumeLinkRegex.Matches(assistantResponse)
			.Select(match => Guid.TryParse(match.Groups["id"].Value, out var id) ? id : Guid.Empty)
			.Where(id => id != Guid.Empty)
			.Distinct()
			.ToList();
		if (referencedIds.Count == 0) return;

		var ownedIds = await context.Perfumes
			.AsNoTracking()
			.Where(perfume => referencedIds.Contains(perfume.Id))
			.Select(perfume => perfume.Id)
			.ToListAsync(cancellationToken);
		if (ownedIds.Count == 0) return;

		conversation.DiscussedPerfumeIds = conversation.DiscussedPerfumeIds
			.Concat(ownedIds)
			.Distinct()
			.ToList();
		await context.SaveChangesAsync(cancellationToken);
	}

	public async Task GenerateAndSaveConversationTitle(ChatConversation conversation, CancellationToken cancellationToken) {
		if (conversation.TitleGeneratedAt.HasValue) return;

		try {
			var visibleMessages = await context.Set<Models.ChatMessage>()
				.IgnoreQueryFilters()
				.AsNoTracking()
				.Where(message => message.ConversationId == conversation.Id)
				.Where(message =>
					message.Role == "user" ||
					(message.Role == "assistant" && message.ChatFinishReason != ChatFinishReason.ToolCalls))
				.OrderBy(message => message.MessageIndex)
				.Select(message => new { message.Role, message.Content })
				.ToListAsync(cancellationToken);

			var transcript = new StringBuilder();
			foreach (var message in visibleMessages) {
				var line = $"{message.Role}: {message.Content}\n";
				if (transcript.Length + line.Length > 6000) break;
				transcript.Append(line);
			}

			if (transcript.Length == 0) return;

			var titleOptions = ChatAgentResponseProtocol.CreateOptions(openAiOptions.Value.AssistantModel, [
				ResponseItem.CreateSystemMessageItem(
					"Create a concise, specific title that summarizes the main topic of this conversation. " +
					"Use 3 to 8 words. Return only the title, without quotes, punctuation at the end, or markdown."),
				ResponseItem.CreateUserMessageItem(transcript.ToString())
			], [], allowTools: false);
			// The output budget includes reasoning tokens as well as the short title.
			titleOptions.MaxOutputTokenCount = 2048;
			var response = (await responsesClient.CreateResponseAsync(titleOptions, cancellationToken)).Value;
			Diagnostics.RecordChatTokenUsage(response, "chat_conversation_title");
			if (response.Status != ResponseStatus.Completed) return;

			var generatedTitle = response.GetOutputText()?.Trim().Trim('"', '\'');
			if (string.IsNullOrWhiteSpace(generatedTitle)) return;

			const string titlePrefix = "Title:";
			if (generatedTitle.StartsWith(titlePrefix, StringComparison.OrdinalIgnoreCase)) {
				generatedTitle = generatedTitle[titlePrefix.Length..].Trim();
			}

			conversation.Title = generatedTitle.Length > 100 ? generatedTitle[..100].TrimEnd() : generatedTitle;
			conversation.TitleGeneratedAt = DateTime.UtcNow;
			await context.SaveChangesAsync(cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (Exception ex) {
			logger.LogWarning(ex, "Could not generate title for chat conversation {ConversationId}", conversation.Id);
		}
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
