namespace PerfumeTracker.Server.Services.Achievements;

public static class SeedAchievements{
	public static async Task SeedAchievementsAsync(PerfumeTrackerContext context) {
		if (!await context.Achievements.AnyAsync()) {
			await context.Achievements.AddRangeAsync(
				perfumesAdded
				.Concat(perfumeWornDays).Concat(tags)
				.Concat(perfumeTags)
				.Concat(streaks));
			await context.SaveChangesAsync();
		}
	}
	
	private readonly static List<Achievement> perfumesAdded = new List<Achievement>() {
		new Achievement() {
			MinPerfumesAdded = 1,
			Name = "First Step",
			Description = "Your first perfume - cherish it!"
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
			Description = "Your collection rivals the great perfume houses of the world"
		},
	};

	private readonly static List<Achievement> perfumeWornDays = new List<Achievement>() {
		new Achievement() {
			MinPerfumeWornDays = 1,
			Name = "First Wear",
			Description = "Baby steps"
		},
		new Achievement() {
			MinPerfumeWornDays = 7,
			Name = "One Week",
			Description = "You're making fragrance a part of your daily routine"
		},
		new Achievement() {
			MinPerfumeWornDays = 50,
			Name = "Dedicated",
			Description = "Your commitment to fragrance is unwavering"
		},
		new Achievement() {
			MinPerfumeWornDays = 100,
			Name = "Fragrance Enthusiast",
			Description = "Now we're cooking"
		},
		new Achievement() {
			MinPerfumeWornDays = 365,
			Name = "Year of Scents",
			Description = "A full year of fragrance adventure"
		},
		new Achievement() {
			MinPerfumeWornDays = 3650,
			Name = "Decade of Discovery",
			Description = "10 full years - how does it feel?"
		},
	};

	private readonly static List<Achievement> tags = new List<Achievement>() {
		new Achievement() {
			MinTags = 1,
			Name = "Tag Creator",
			Description = "You've created your first tag"
		},
		new Achievement() {
			MinTags = 10,
			Name = "Organizer",
			Description = "You're starting to organize your collection"
		},
		new Achievement() {
			MinTags = 25,
			Name = "Taxonomist",
			Description = "Your classification system is getting sophisticated"
		},
		new Achievement() {
			MinTags = 100,
			Name = "Master Classifier",
			Description = "Your tag system is a work of art"
		},
	};

	private readonly static List<Achievement> perfumeTags = new List<Achievement>() {
		new Achievement() {
			MinPerfumeTags = 1,
			Name = "First Tag",
			Description = "You've tagged your first perfume"
		},
		new Achievement() {
			MinPerfumeTags = 50,
			Name = "Tagging Pro",
			Description = "You're getting good at organizing your collection"
		},
		new Achievement() {
			MinPerfumeTags = 250,
			Name = "Collection Curator",
			Description = "Your collection is well-organized and documented"
		},
		new Achievement() {
			MinPerfumeTags = 5000,
			Name = "Master Organizer",
			Description = "You know your perfumes like the back of your hand"
		},
	};

	private readonly static List<Achievement> streaks = new List<Achievement>() {
		new Achievement() {
			MinStreak = 3,
			Name = "Getting Started",
			Description = "Three days in a row!"
		},
		new Achievement() {
			MinStreak = 7,
			Name = "Week Warrior",
			Description = "A full week of fragrance"
		},
		new Achievement() {
			MinStreak = 30,
			Name = "Monthly Master",
			Description = "A month of dedication"
		},
		new Achievement() {
			MinStreak = 100,
			Name = "Century Club",
			Description = "100 days of unwavering attention"
		},
		new Achievement() {
			MinStreak = 365,
			Name = "Year of Dedication",
			Description = "A full year - your dedication is unparallelled!"
		},
	};
}
