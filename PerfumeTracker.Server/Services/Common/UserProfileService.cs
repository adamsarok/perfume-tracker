using Microsoft.Extensions.Caching.Memory;

namespace PerfumeTracker.Server.Services.Common;

public class UserProfileService(PerfumeTrackerContext context, IMemoryCache memoryCache) : IUserProfileService {
	private const string CacheKey = "UserProfile";
	private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
	public async Task<UserProfile> GetCurrentUserProfile(CancellationToken cancellationToken) {
		return await memoryCache.GetOrCreateAsync(CacheKey, async entry => {
			entry.AbsoluteExpirationRelativeToNow = CacheExpiration;
			return await context.UserProfiles.FirstAsync(cancellationToken);
		}) ?? throw new InvalidOperationException("User profile not found");
	}
}
