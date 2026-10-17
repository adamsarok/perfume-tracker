namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public interface IChatSystemPromptBuilder {
	Task<string> BuildAsync(CancellationToken cancellationToken);
}
