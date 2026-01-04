using OpenAI.Chat;

namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public interface IChatAgentTools {
	Task<string> ExecuteToolCall(ChatToolCall toolCall, CancellationToken cancellationToken);
}
