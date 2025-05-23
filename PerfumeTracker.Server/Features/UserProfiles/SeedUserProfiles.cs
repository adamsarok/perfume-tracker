namespace PerfumeTracker.Server.Features.UserProfiles;

public static class SeedUserProfiles {
	private static UserProfile seed = new UserProfile() {
		UserId = PerfumeTrackerContext.DEFAULT_USERID,
		UserName = PerfumeTrackerContext.DEFAULT_USERID,
		Email = "",
		MinimumRating = 8,
		DayFilter = 30,
		ShowFemalePerfumes = true,
		ShowMalePerfumes = true,
		ShowUnisexPerfumes = true,
		SprayAmountFullSizeMl = 0.2M, //0.1m * 2 sprays
		SprayAmountSamplesMl = 0.1M, //0.5m * 2 sprays
	};
	public static async Task DoSeed(PerfumeTrackerContext context) {
		if (!await context.UserProfiles.AnyAsync()) {
			await context.UserProfiles.AddRangeAsync(seed);
			await context.SaveChangesAsync();
		}
	}
}
