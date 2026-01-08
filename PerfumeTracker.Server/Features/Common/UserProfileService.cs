using Microsoft.Extensions.Caching.Memory;

namespace PerfumeTracker.Server.Features.Common;

public class UserProfileService(PerfumeTrackerContext context, IMemoryCache memoryCache) : IUserProfileService {
	private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
	public async Task<UserProfile> GetCurrentUserProfile(CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		return await memoryCache.GetOrCreateAsync(userId, async entry => {
			entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
			return await context.UserProfiles.FirstAsync(cancellationToken);
		}) ?? throw new InvalidOperationException("User profile not found");
	}

	public void InvalidateCache() {
		var userId = context.TenantProvider?.GetCurrentUserId();
		if (userId != null) memoryCache.Remove(userId);
	}
}
