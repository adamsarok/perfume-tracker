using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.Server.Features.ChatAgent;

public record ChatWithAgentCommand(Guid? ConversationId, string Message) : ICommand<ChatAgentResponse>;
public record GetConversationQuery(Guid ConversationId) : IQuery<GetConversationResult>;
public record GetConversationsQuery() : IQuery<IEnumerable<ChatConversationSummaryDto>>;
public record GetConversationResult(ChatConversationDto? Conversation);
public record ChatConversationSummaryDto(Guid Id, string? Title, IEnumerable<Guid> DiscussedPerfumeIds, DateTime CreatedAt, DateTime UpdatedAt);
public record ChatConversationDto(
	Guid Id,
	string? Title,
	IEnumerable<Guid> DiscussedPerfumeIds,
	IEnumerable<ChatMessageDto> Messages,
	DateTime CreatedAt,
	DateTime UpdatedAt);
public record ChatMessageDto(
	Guid Id,
	Guid ConversationId,
	string Role,
	string Content,
	int MessageIndex,
	DateTime CreatedAt);

public class ChatWithAgentEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/chat", async (ChatWithAgentCommand command, ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(command, cancellationToken);
			return Results.Ok(result);
		})
		.WithTags("Chat")
		.WithName("ChatWithAgent")
		.RequireAuthorization(Policies.READ);

		app.MapGet("/api/chat/conversations", async (ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(new GetConversationsQuery(), cancellationToken);
			return Results.Ok(result);
		})
		.WithTags("Chat")
		.WithName("GetConversations")
		.RequireAuthorization(Policies.READ);

		app.MapGet("/api/chat/conversations/{conversationId:guid}", async (Guid conversationId, ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(new GetConversationQuery(conversationId), cancellationToken);
			return result.Conversation != null ? Results.Ok(result.Conversation) : Results.NotFound();
		})
		.WithTags("Chat")
		.WithName("GetConversation")
		.RequireAuthorization(Policies.READ);
	}
}

public class ChatWithAgentCommandValidator : AbstractValidator<ChatWithAgentCommand> {
	public ChatWithAgentCommandValidator() {
		RuleFor(x => x.Message).NotEmpty().MaximumLength(2000);
	}
}

public class ChatWithAgentHandler(IChatAgent chatAgent) : ICommandHandler<ChatWithAgentCommand, ChatAgentResponse> {
	public async Task<ChatAgentResponse> Handle(ChatWithAgentCommand request, CancellationToken cancellationToken) {
		var chatRequest = new ChatAgentRequest(request.ConversationId, request.Message);
		return await chatAgent.ChatAsync(chatRequest, cancellationToken);
	}
}

public class GetConversationHandler(IChatAgent chatAgent) : IQueryHandler<GetConversationQuery, GetConversationResult> {
	public async Task<GetConversationResult> Handle(GetConversationQuery request, CancellationToken cancellationToken) {
		var conversation = await chatAgent.GetConversationAsync(request.ConversationId, cancellationToken);
		return new GetConversationResult(conversation == null ? null : new ChatConversationDto(
			conversation.Id,
			conversation.Title,
			conversation.DiscussedPerfumeIds,
			conversation.Messages
				.OrderBy(message => message.MessageIndex)
				.Where(message =>
					message.Role == "user" ||
					(message.Role == "assistant" && message.ChatFinishReason != OpenAI.Chat.ChatFinishReason.ToolCalls))
				.Select(message => new ChatMessageDto(
					message.Id,
					message.ConversationId,
					message.Role,
					message.Content,
					message.MessageIndex,
					message.CreatedAt)),
			conversation.CreatedAt,
			conversation.UpdatedAt));
	}
}

public class GetConversationsHandler(IChatAgent chatAgent) : IQueryHandler<GetConversationsQuery, IEnumerable<ChatConversationSummaryDto>> {
	public async Task<IEnumerable<ChatConversationSummaryDto>> Handle(GetConversationsQuery request, CancellationToken cancellationToken) {
		var conversations = await chatAgent.GetUserConversationsAsync(cancellationToken);
		return conversations.Select(conversation => new ChatConversationSummaryDto(
			conversation.Id,
			conversation.Title,
			conversation.DiscussedPerfumeIds,
			conversation.CreatedAt,
			conversation.UpdatedAt));
	}
}
