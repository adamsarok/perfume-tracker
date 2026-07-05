using OpenAI.Chat;

namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public interface IChatAgentTools {
	IReadOnlyList<ChatTool> Tools { get; }
	Task<string> ExecuteToolCall(ChatToolCall toolCall, CancellationToken cancellationToken);
}
