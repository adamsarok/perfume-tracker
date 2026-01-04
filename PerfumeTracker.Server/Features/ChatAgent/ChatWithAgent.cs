using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.Server.Features.ChatAgent;

public record ChatWithAgentCommand(Guid? ConversationId, string Message) : ICommand<ChatAgentResponse>;
public record GetConversationQuery(Guid ConversationId) : IQuery<Models.ChatConversation?>;
public record GetConversationsQuery() : IQuery<IEnumerable<Models.ChatConversation>>;

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
			return result != null ? Results.Ok(result) : Results.NotFound();
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

public class GetConversationHandler(IChatAgent chatAgent) : IQueryHandler<GetConversationQuery, Models.ChatConversation?> {
	public async Task<Models.ChatConversation?> Handle(GetConversationQuery request, CancellationToken cancellationToken) {
		return await chatAgent.GetConversationAsync(request.ConversationId, cancellationToken);
	}
}

public class GetConversationsHandler(IChatAgent chatAgent) : IQueryHandler<GetConversationsQuery, IEnumerable<Models.ChatConversation>> {
	public async Task<IEnumerable<Models.ChatConversation>> Handle(GetConversationsQuery request, CancellationToken cancellationToken) {
		return await chatAgent.GetUserConversationsAsync(cancellationToken);
	}
}
