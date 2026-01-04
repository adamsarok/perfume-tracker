namespace PerfumeTracker.Server.Features.Perfumes.Services;

public interface ISystemPromptCache {
	Task<string> GetOrBuildSystemPromptAsync(Guid userId, Func<Task<string>> buildPrompt);
	void InvalidateCache(Guid userId);
}
