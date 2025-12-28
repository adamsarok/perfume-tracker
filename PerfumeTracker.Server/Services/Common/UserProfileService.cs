
namespace PerfumeTracker.Server.Services.Common;

public class UserProfileService(PerfumeTrackerContext context) : IUserProfileService {
	public async Task<UserProfile> GetCurrentUserProfile(CancellationToken cancellationToken) {
		return await context.UserProfiles.FirstAsync();
	}
}
