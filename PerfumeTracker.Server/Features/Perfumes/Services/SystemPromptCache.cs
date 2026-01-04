using Microsoft.Extensions.Caching.Memory;

namespace PerfumeTracker.Server.Features.Perfumes.Services;

public class SystemPromptCache(IMemoryCache memoryCache) : ISystemPromptCache {
	private const string CacheKeyPrefix = "system_prompt";

	public async Task<string> GetOrBuildSystemPromptAsync(Guid userId, Func<Task<string>> buildPrompt) {
		var cacheKey = $"{CacheKeyPrefix}::{userId}";

		return await memoryCache.GetOrCreateAsync(cacheKey, async entry => {
			entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
			entry.SlidingExpiration = TimeSpan.FromHours(1);
			return await buildPrompt();
		}) ?? throw new InvalidOperationException("Failed to build system prompt");
	}

	public void InvalidateCache(Guid userId) {
		var cacheKey = $"{CacheKeyPrefix}::{userId}";
		memoryCache.Remove(cacheKey);
	}
	public class PerfumeAddedNotificationHandler(ISystemPromptCache systemPromptCache) : INotificationHandler<PerfumeAddedNotification> {
		public Task Handle(PerfumeAddedNotification notification, CancellationToken cancellationToken) {
			systemPromptCache.InvalidateCache(notification.UserId);
			return Task.CompletedTask;
		}
	}
	// TODO no deleted notification yet
	//public class PerfumeDeletedNotificationHandler(ISystemPromptCache systemPromptCache) : INotificationHandler<PerfumeDeletedNotification> {
	//	public Task Handle(PerfumeAddedNotification notification, CancellationToken cancellationToken) {
	//		systemPromptCache.InvalidateCache(notification.UserId);
	//		return Task.CompletedTask;
	//	}
	//}
}
