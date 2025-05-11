namespace PerfumeTracker.Server.Features.Achievements;

public class SeedAchievements(PerfumeTrackerContext context) {
	public async Task SeedAchievementss() {
		if (!await context.Achievements.AnyAsync()) {
			await context.Achievements.AddRangeAsync(seed);
			await context.SaveChangesAsync();
		}
	}
	private static List<Achievement> seed = new List<Achievement>() {

	};
}
