using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Models;
using System.Threading.Tasks;
using static PerfumeTracker.Server.Features.Missions.ProgressMissions;

namespace PerfumeTracker.Server.Features.Missions;

public class MissionService(IServiceProvider serviceProvider, ILogger<MissionService> logger) : BackgroundService {
	private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			try {
				using var scope = serviceProvider.CreateScope();
				var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
				await CreateWeeklyMissionsAsync(context);
			} catch (Exception ex) {
				logger.LogError(ex, "CreateWeeklyMissionsAsync failed");
			}
			await Task.Delay(_checkInterval, stoppingToken);
		}
	}

	private async Task CreateWeeklyMissionsAsync(PerfumeTrackerContext context) {
		var now = DateTime.UtcNow;
		var startDate = now.Date.AddDays(-((int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1));
		var endDate = startDate.AddDays(7);

		await context.Missions
			.Where(m => m.EndDate < now)
			.ExecuteUpdateAsync(s => s.SetProperty(m => m.IsActive, false));

		if (!await context.Missions.AnyAsync(m => m.StartDate == startDate && m.IsActive)) {
			var missions = await GenerateRandomMissions(3, context);

			foreach (var mission in missions) {
				mission.StartDate = startDate;
				mission.EndDate = endDate;
				mission.IsActive = true;
			}

			await context.Missions.AddRangeAsync(missions);
			await context.SaveChangesAsync();
		}
	}

	private async Task<List<Mission>> GenerateRandomMissions(int count, PerfumeTrackerContext context) {
		var random = new Random();
		var missionTypes = Enum.GetValues<MissionType>().ToList();
		var selectedTypes = new HashSet<MissionType>();
		var missions = new List<Mission>();

		while (missions.Count < count && selectedTypes.Count < missionTypes.Count) {
			var availableTypes = missionTypes.Where(t => !selectedTypes.Contains(t)).ToList();
			var selectedType = availableTypes[random.Next(availableTypes.Count)];
			selectedTypes.Add(selectedType);

			var mission = selectedType switch {
				MissionType.WearPerfumes => new Mission {
					Name = "Dedication",
					Description = "Wear a perfume every day this week",
					Type = MissionType.WearPerfumes,
					RequiredCount = 7,
					XP = 100
				},
				MissionType.WearSamePerfume => new Mission {
					Name = "Loyalty",
					Description = "Wear the same perfume 5 times this week",
					Type = MissionType.WearSamePerfume,
					RequiredCount = 5,
					XP = 75
				},
				MissionType.GetRandoms => new Mission {
					Name = "Adventurer",
					Description = "Get 15 random perfume suggestions",
					Type = MissionType.GetRandoms,
					RequiredCount = 15,
					XP = 50
				},
				MissionType.AcceptRandoms => new Mission {
					Name = "Lady Luck",
					Description = "Take a chance! Accept 3 perfume suggestions",
					Type = MissionType.AcceptRandoms,
					RequiredCount = 3,
					XP = 100
				},
				MissionType.PerfumesTaggedPhotographed => new Mission {
					Name = "Curator",
					Description = "Tag and photograph all your perfumes",
					Type = MissionType.PerfumesTaggedPhotographed,
					RequiredCount = 0,
					XP = 100
				},
				MissionType.UseUnusedPerfumes => new Mission {
					Name = "Rediscovery",
					Description = "Wear 2 perfumes you haven't used in a while",
					Type = MissionType.UseUnusedPerfumes,
					RequiredCount = 2,
					XP = 100
				},
				MissionType.WearDifferentPerfumes => new Mission {
					Name = "Variety",
					Description = "Wear 5 different perfumes this week",
					Type = MissionType.WearDifferentPerfumes,
					RequiredCount = 5,
					XP = 75
				},
				MissionType.WearNote => await GetWearMissionAsync(context),
				_ => throw new ArgumentException($"Unhandled mission type: {selectedType}")
			};

			if (mission != null) missions.Add(mission);
		}

		return missions;
	}

	private async Task<Mission?> GetWearMissionAsync(PerfumeTrackerContext context) {
		var randomTag = await context.Tags
			.OrderBy(t => EF.Functions.Random())
			.FirstOrDefaultAsync();
		if (randomTag == null) return null;
		return new Mission {
			Name = "Note Hunter",
			Description = $"Wear perfumes with a {randomTag.TagName} note",
			Type = MissionType.WearNote,
			RequiredCount = 3,
			RequiredId = randomTag.Id,
			XP = 50
		};
	}
}
