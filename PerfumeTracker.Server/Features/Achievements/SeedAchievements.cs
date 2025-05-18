namespace PerfumeTracker.Server.Features.Achievements;

public static class SeedAchievements{
	public static async Task DoSeed(PerfumeTrackerContext context) {
		if (!await context.Achievements.AnyAsync()) {
			await context.Achievements.AddRangeAsync(perfumesAdded);
			await context.Achievements.AddRangeAsync(perfumeWornDays);
			await context.Achievements.AddRangeAsync(tags);
			await context.Achievements.AddRangeAsync(perfumeTags);
			await context.Achievements.AddRangeAsync(streaks);
			await context.Achievements.AddRangeAsync(xpMilestones);
			await context.SaveChangesAsync();
		}
	}
	//TODO: total ml - hoarding, expand achievement types

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

	private readonly static List<Achievement> xpMilestones = new List<Achievement>() {
		new Achievement() {
			MinXP = 100,
			Name = "Level 1",
			Description = "Level 1"
		},
		new Achievement() {
			MinXP = 250,
			Name = "Level 2",
			Description = "Level 2"
		},
		new Achievement() {
			MinXP = 500,
			Name = "Level 3",
			Description = "Level 3"
		},
		new Achievement() {
			MinXP = 1000,
			Name = "Level 4",
			Description = "Level 4"
		},
		new Achievement() {
			MinXP = 2000,
			Name = "Level 5",
			Description = "Level 5"
		},
		new Achievement() {
			MinXP = 3500,
			Name = "Level 6",
			Description = "Level 6"
		},
		new Achievement() {
			MinXP = 5000,
			Name = "Level 7",
			Description = "Level 7"
		},
		new Achievement() {
			MinXP = 7000,
			Name = "Level 8",
			Description = "Level 8"
		},
		new Achievement() {
			MinXP = 10000,
			Name = "Level 9",
			Description = "Level 9"
		},
		new Achievement() {
			MinXP = 14000,
			Name = "Level 10",
			Description = "Level 10"
		},
		new Achievement() {
			MinXP = 18000,
			Name = "Level 11",
			Description = "Level 11"
		},
		new Achievement() {
			MinXP = 22000,
			Name = "Level 12",
			Description = "Level 12"
		},
		new Achievement() {
			MinXP = 27000,
			Name = "Level 13",
			Description = "Level 13"
		},
		new Achievement() {
			MinXP = 32000,
			Name = "Level 14",
			Description = "Level 14"
		},
		new Achievement() {
			MinXP = 37000,
			Name = "Level 15",
			Description = "Level 15"
		},
		new Achievement() {
			MinXP = 42000,
			Name = "Level 16",
			Description = "Level 16"
		},
		new Achievement() {
			MinXP = 45000,
			Name = "Level 17",
			Description = "Level 17"
		},
		new Achievement() {
			MinXP = 47000,
			Name = "Level 18",
			Description = "Level 18"
		},
		new Achievement() {
			MinXP = 48500,
			Name = "Level 19",
			Description = "Level 19"
		},
		new Achievement() {
			MinXP = 50000,
			Name = "Level 20",
			Description = "Level 20"
		},
	};
}
