namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public record ChatAgentRequest(Guid? ConversationId, string UserMessage);
public record ChatAgentResponse(Guid ConversationId, string AssistantMessage);

public interface IChatAgent {
	Task<ChatAgentResponse> ChatAsync(ChatAgentRequest request, CancellationToken cancellationToken);
	Task<Models.ChatConversation?> GetConversationAsync(Guid conversationId, CancellationToken cancellationToken);
	Task<IEnumerable<Models.ChatConversation>> GetUserConversationsAsync(CancellationToken cancellationToken);
}
