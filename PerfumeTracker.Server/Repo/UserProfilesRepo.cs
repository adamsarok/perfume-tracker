namespace PerfumeTracker.Server.Repo;

public class UserProfilesRepo(PerfumeTrackerContext context) {
	private readonly UserProfile defaultUserProfile = new UserProfile() {
		UserName = "DEFAULT",
		Email = "",
		MinimumRating = 8,
		DayFilter = 30,
		ShowFemalePerfumes = true,
		ShowMalePerfumes = true,
		ShowUnisexPerfumes = true,
		SprayAmountFullSizeMl = 0.2M, //0.1m * 2 sprays
		SprayAmountSamplesMl = 0.1M, //0.5m * 2 sprays
	};
	private async Task<UserProfile?> GetUserProfile() {
        return await context.UserProfiles.FirstOrDefaultAsync();
    }
	public async Task<UserProfile> GetUserProfileOrDefault() {
		var result = await context.UserProfiles.FirstOrDefaultAsync();
		if (result != null) return result;
		return defaultUserProfile;
	}
	public async Task<UserProfile> UpsertUserProfile(UserProfile userProfile) {
        var found = await GetUserProfile();
        if (found != null) {
            context.Entry(found).CurrentValues.SetValues(userProfile);
        } else {
            await context.UserProfiles.AddAsync(userProfile);
        }
        await context.SaveChangesAsync();
        return userProfile;
    }
}
