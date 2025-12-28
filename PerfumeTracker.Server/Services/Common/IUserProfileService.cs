namespace PerfumeTracker.Server.Services.Common;

public interface IUserProfileService {
	Task<UserProfile> GetCurrentUserProfile(CancellationToken cancellationToken);
}
