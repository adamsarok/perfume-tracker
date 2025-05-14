namespace PerfumeTracker.Server.Features.Achievements;

public class SeedAchievements() {
	public static async Task DoSeed(PerfumeTrackerContext context) {
		if (!await context.Achievements.AnyAsync()) {
			await context.Achievements.AddRangeAsync(seed);
			await context.SaveChangesAsync();
		}
	}
	//TODO: total ml - hoarding, expand achievement types

	public static List<Achievement> seed = new List<Achievement>() {
		new Achievement() {
			MinPerfumesAdded = 1,
			Name = "First Step",
			Description = "Congrats, you've started your fragrance journey!"
		},
		new Achievement() {
			MinPerfumesAdded = 5,
			Name = "Dabbler",
			Description = "Dabbling it up"
		},
		new Achievement() {
			MinPerfumesAdded = 25,
			Name = "Journeyman",
			Description = "This is getting serious"
		},
		new Achievement() {
			MinPerfumesAdded = 100,
			Name = "Curator",
			Description = "You know what you like and you're not afraid to wear it"
		},
		new Achievement() {
			MinPerfumesAdded = 500,
			Name = "Librarian",
			Description = "Knowledge is power"
		},
		new Achievement() {
			MinPerfumesAdded = 1000,
			Name = "Grandmaster",
			Description = "You have sampled new and vintage, every note and every combination - or did you?"
		},
		new Achievement() {
			MinPerfumesAdded = 5000,
			Name = "Scent God",
			Description = ""
		},
	};
}
