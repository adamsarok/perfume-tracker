using Microsoft.Extensions.Caching.Memory;
using Moq;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Features.Perfumes.Services;
using static PerfumeTracker.Server.Features.Perfumes.Services.SystemPromptCache;

namespace PerfumeTracker.xTests.Tests;

public class SystemPromptCacheTests {

	[Fact]
	public async Task GetOrBuildSystemPromptAsync_CachesResult() {
		var cache = new MemoryCache(new MemoryCacheOptions());
		var systemPromptCache = new SystemPromptCache(cache);
		var userId = Guid.NewGuid();
		var callCount = 0;

		var result1 = await systemPromptCache.GetOrBuildSystemPromptAsync(userId, () => {
			callCount++;
			return Task.FromResult("prompt-content");
		});
		var result2 = await systemPromptCache.GetOrBuildSystemPromptAsync(userId, () => {
			callCount++;
			return Task.FromResult("different-content");
		});

		Assert.Equal("prompt-content", result1);
		Assert.Equal("prompt-content", result2);
		Assert.Equal(1, callCount);
	}

	[Fact]
	public async Task InvalidateCache_RemovesCachedEntry() {
		var cache = new MemoryCache(new MemoryCacheOptions());
		var systemPromptCache = new SystemPromptCache(cache);
		var userId = Guid.NewGuid();

		await systemPromptCache.GetOrBuildSystemPromptAsync(userId, () => Task.FromResult("original"));
		systemPromptCache.InvalidateCache(userId);
		var result = await systemPromptCache.GetOrBuildSystemPromptAsync(userId, () => Task.FromResult("rebuilt"));

		Assert.Equal("rebuilt", result);
	}

	[Fact]
	public async Task PerfumeAddedNotificationHandler_InvalidatesCache() {
		var mockCache = new Mock<ISystemPromptCache>();
		var handler = new PerfumeAddedNotificationHandler(mockCache.Object);
		var userId = Guid.NewGuid();
		var notification = new PerfumeAddedNotification(Guid.NewGuid(), userId);

		await handler.Handle(notification, CancellationToken.None);

		mockCache.Verify(c => c.InvalidateCache(userId), Times.Once);
	}
}
