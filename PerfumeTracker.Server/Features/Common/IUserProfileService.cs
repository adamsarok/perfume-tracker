namespace PerfumeTracker.Server.Features.Common;

public interface IUserProfileService {
	Task<UserProfile> GetCurrentUserProfile(CancellationToken cancellationToken);
	void InvalidateCache();
}
