using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Features.Demo;

public static class SeedDemoData {
	public static async Task SeedDemoDataAsync(PerfumeTrackerContext context, Guid demoUserId, List<Guid> demoImageGuids) {
		await SeedDemoTagsAsync(context, demoUserId);
		await SeedDemoPerfumesAsync(context, demoUserId, demoImageGuids);
		await SeedDemoWearEventsAsync(context, demoUserId);
		await SeedDemoMissionsAsync(context, demoUserId);
	}

	private static async Task SeedDemoTagsAsync(PerfumeTrackerContext context, Guid demoUserId) {
		if (await context.Tags.IgnoreQueryFilters().AnyAsync(t => t.UserId == demoUserId))
			return;

		var demoTags = new List<Tag>
		{
			new() { UserId = demoUserId, TagName = "Fresh", Color = "#4CAF50" },
			new() { UserId = demoUserId, TagName = "Floral", Color = "#E91E63" },
			new() { UserId = demoUserId, TagName = "Woody", Color = "#8D6E63" },
			new() { UserId = demoUserId, TagName = "Citrus", Color = "#FF9800" },
			new() { UserId = demoUserId, TagName = "Oriental", Color = "#9C27B0" },
			new() { UserId = demoUserId, TagName = "Aromatic", Color = "#2196F3" },
			new() { UserId = demoUserId, TagName = "Chypre", Color = "#795548" },
			new() { UserId = demoUserId, TagName = "Gourmand", Color = "#FF5722" },
			new() { UserId = demoUserId, TagName = "Office", Color = "#9E9E9E" },
			new() { UserId = demoUserId, TagName = "Casual", Color = "#4CAF50" },
			new() { UserId = demoUserId, TagName = "Formal", Color = "#3F51B5" },
			new() { UserId = demoUserId, TagName = "Vintage", Color = "#FFC107" }
		};

		await context.Tags.AddRangeAsync(demoTags);
		await context.SaveChangesAsync();
	}

	private static async Task SeedDemoPerfumesAsync(PerfumeTrackerContext context, Guid demoUserId, List<Guid> demoImageGuids) {
		if (await context.Perfumes.IgnoreQueryFilters().AnyAsync(p => p.UserId == demoUserId))
			return;

		var tags = await context.Tags
			.IgnoreQueryFilters()
			.Where(t => t.UserId == demoUserId).ToListAsync();
		var tagDict = tags.ToDictionary(t => t.TagName, t => t);

		int actImg = 0;

		Guid? GetDemoImageGuid() { 
			if (demoImageGuids == null || demoImageGuids.Count == 0) return null;
			if (actImg >= demoImageGuids.Count) actImg = 0;
			return demoImageGuids[actImg++];
		};

		Perfume azure = new() {
			UserId = demoUserId,
			House = "Maison de Luxe",
			PerfumeName = "Azure Mystique",
			Ml = 100,
			MlLeft = 85,
			ImageObjectKeyNew = GetDemoImageGuid(),
			Autumn = true,
			Spring = true,
			Summer = true,
			Winter = true
		};
		Perfume tobacco = new() {
			UserId = demoUserId,
			House = "Atelier de Senteurs",
			PerfumeName = "Tobacco Rêve",
			Ml = 50,
			MlLeft = 45,
			ImageObjectKeyNew = GetDemoImageGuid(),
			Autumn = true,
			Spring = false,
			Summer = false,
			Winter = true
		};
		Perfume cool = new() {
			UserId = demoUserId,
			House = "Maison de Joie",
			PerfumeName = "Cool Magic",
			Ml = 100,
			MlLeft = 60,
			ImageObjectKeyNew = GetDemoImageGuid(),
			Autumn = false,
			Spring = true,
			Summer = true,
			Winter = false
		};
		Perfume nocturne = new() {
			UserId = demoUserId,
			House = "Maison Marguerite",
			PerfumeName = "Nocturne",
			Ml = 100,
			MlLeft = 80,
			ImageObjectKeyNew = GetDemoImageGuid(),
			Autumn = true,
			Spring = false,
			Summer = false,
			Winter = true
		};
		Perfume bergamot = new() {
			UserId = demoUserId,
			House = "Acqua d'Italia",
			PerfumeName = "Classic Bergamot",
			Ml = 100,
			MlLeft = 90,
			ImageObjectKeyNew = GetDemoImageGuid(),
			Autumn = false,
			Spring = true,
			Summer = true,
			Winter = false
		};
		await context.Perfumes.AddRangeAsync(azure, tobacco, cool, bergamot, nocturne);

		var perfumeTags = new List<PerfumeTag>();
		perfumeTags.AddRange(new[]
			{
			new PerfumeTag { PerfumeId = azure.Id, TagId = tagDict["Woody"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = azure.Id, TagId = tagDict["Aromatic"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = azure.Id, TagId = tagDict["Office"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = azure.Id, TagId = tagDict["Formal"].Id, UserId = demoUserId }
		});
		perfumeTags.AddRange(new[]
		{
			new PerfumeTag { PerfumeId = tobacco.Id, TagId = tagDict["Oriental"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = tobacco.Id, TagId = tagDict["Gourmand"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = tobacco.Id, TagId = tagDict["Casual"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = tobacco.Id, TagId = tagDict["Formal"].Id, UserId = demoUserId }
		});
		perfumeTags.AddRange(new[]
		{
			new PerfumeTag { PerfumeId = cool.Id, TagId = tagDict["Fresh"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = cool.Id, TagId = tagDict["Casual"].Id, UserId = demoUserId }
		});
		perfumeTags.AddRange(new[]
		{
			new PerfumeTag { PerfumeId = nocturne.Id, TagId = tagDict["Oriental"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = nocturne.Id, TagId = tagDict["Gourmand"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = nocturne.Id, TagId = tagDict["Casual"].Id, UserId = demoUserId }
		});
		perfumeTags.AddRange(new[]
		{
			new PerfumeTag { PerfumeId = bergamot.Id, TagId = tagDict["Citrus"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = bergamot.Id, TagId = tagDict["Fresh"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = bergamot.Id, TagId = tagDict["Vintage"].Id, UserId = demoUserId },
			new PerfumeTag { PerfumeId = bergamot.Id, TagId = tagDict["Casual"].Id, UserId = demoUserId }
		});
		await context.PerfumeTags.AddRangeAsync(perfumeTags);
		await context.SaveChangesAsync();
	}

	private static async Task SeedDemoWearEventsAsync(PerfumeTrackerContext context, Guid demoUserId) {
		if (await context.PerfumeEvents.IgnoreQueryFilters().AnyAsync(pe => pe.UserId == demoUserId))
			return;

		var perfumes = await context.Perfumes.IgnoreQueryFilters().Where(p => p.UserId == demoUserId).ToListAsync();
		var wearEvents = new List<PerfumeEvent>();

		var random = new Random(42);
		var today = DateTime.UtcNow.Date;
		foreach (var perfume in perfumes) {
			wearEvents.Add(new PerfumeEvent {
				UserId = demoUserId,
				PerfumeId = perfume.Id,
				EventDate = perfume.CreatedAt,
				Type = PerfumeEvent.PerfumeEventType.Added,
				AmountMl = 0,
			});
			var wearCount = random.Next(1, 8);
			for (int i = 0; i < wearCount; i++) {
				var daysAgo = random.Next(0, 30);
				var wearDate = today.AddDays(-daysAgo);
				var amountMl = random.Next(1, 4);

				wearEvents.Add(new PerfumeEvent {
					UserId = demoUserId,
					PerfumeId = perfume.Id,
					EventDate = wearDate,
					Type = PerfumeEvent.PerfumeEventType.Worn,
					AmountMl = amountMl,
				});
			}
		}
		await context.PerfumeEvents.AddRangeAsync(wearEvents);
		await context.SaveChangesAsync();
	}

	private static async Task SeedDemoMissionsAsync(PerfumeTrackerContext context, Guid demoUserId) {
		if (await context.UserMissions.IgnoreQueryFilters().AnyAsync(um => um.UserId == demoUserId))
			return;
		var random = new Random(42);
		//var demoMissions = new List<Mission>
		//{
		//	new()
		//	{
		//		Name = "Weekend Warrior",
		//		Description = "Wear 3 different perfumes this weekend",
		//		StartDate = DateTime.UtcNow.Date.AddDays(-7),
		//		EndDate = DateTime.UtcNow.Date.AddDays(7),
		//		XP = 50,
		//		Type = MissionType.WearDifferentPerfumes,
		//		RequiredCount = 3,
		//		IsActive = true
		//	},
		//	new()
		//	{
		//		Name = "Office Explorer",
		//		Description = "Wear office-appropriate fragrances 5 times",
		//		StartDate = DateTime.UtcNow.Date.AddDays(-14),
		//		EndDate = DateTime.UtcNow.Date.AddDays(14),
		//		XP = 75,
		//		Type = MissionType.WearPerfumes,
		//		RequiredCount = 5,
		//		IsActive = true
		//	},
		//	new()
		//	{
		//		Name = "Tag Master",
		//		Description = "Tag 10 perfumes with appropriate tags",
		//		StartDate = DateTime.UtcNow.Date.AddDays(-10),
		//		EndDate = DateTime.UtcNow.Date.AddDays(20),
		//		XP = 100,
		//		Type = MissionType.PerfumesTaggedPhotographed,
		//		RequiredCount = 10,
		//		IsActive = true
		//	}
		//};

		//await context.Missions.AddRangeAsync(demoMissions);
		//await context.SaveChangesAsync();

		// Create user missions with some progress
		var userMissions = new List<UserMission>();
		//var missions = await context.Missions.Where(m => .Select(dm => dm.Id).Contains(m.Id)).ToListAsync();

		//foreach (var mission in missions) {
		//	var progress = mission.Name switch {
		//		"Weekend Warrior" => 2, // 2/3 completed
		//		"Office Explorer" => 3,  // 3/5 completed
		//		"Tag Master" => 7,       // 7/10 completed
		//		_ => 0
		//	};

		//	var isCompleted = progress >= mission.RequiredCount;

		//	userMissions.Add(new UserMission {
		//		UserId = demoUserId,
		//		MissionId = mission.Id,
		//		Progress = progress,
		//		IsCompleted = isCompleted,
		//		CompletedAt = isCompleted ? DateTime.UtcNow.AddDays(-random.Next(1, 5)) : null
		//	});
		//}

		//await context.UserMissions.AddRangeAsync(userMissions);
		//await context.SaveChangesAsync();
	}
}