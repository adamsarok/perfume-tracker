using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Achievements;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Achievement Tests")]
public class AchievementCollection : ICollectionFixture<AchievementFixture>;

public class AchievementFixture : DbFixture {
	public AchievementFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await context.Database.ExecuteSqlRawAsync("truncate table \"public\".\"UserAchievement\" cascade");
		await context.Database.ExecuteSqlRawAsync("truncate table \"public\".\"Achievement\" cascade");
		var achievements = new List<Achievement> {
			new() { Id = Guid.NewGuid(), Name = "First Step", Description = "First perfume", MinPerfumesAdded = 1 },
			new() { Id = Guid.NewGuid(), Name = "Dabbler", Description = "5 perfumes", MinPerfumesAdded = 5 },
		};
		await context.Achievements.AddRangeAsync(achievements);
		await context.SaveChangesAsync();
		await SeedPerfumes(3);
	}
}

[Collection("Achievement Tests")]
public class AchievementTests {
	private readonly AchievementFixture _fixture;
	public AchievementTests(AchievementFixture fixture) => _fixture = fixture;

	[Fact]
	public async Task CompleteAchievements_AwardsPerfumeCountAchievements() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var logger = Mock.Of<ILogger<CompleteAchievements.PerfumeAddNotificationHandler>>();
		var handler = new CompleteAchievements.PerfumeAddNotificationHandler(context, logger);
		var userId = _fixture.TenantProvider.MockTenantId!.Value;

		await handler.Handle(
			new PerfumeAddedNotification(Guid.NewGuid(), userId),
			TestContext.Current.CancellationToken);

		var userAchievements = await context.UserAchievements.ToListAsync(TestContext.Current.CancellationToken);
		var achievementIds = userAchievements.Select(ua => ua.AchievementId).ToList();
		var achievements = await context.Achievements
			.IgnoreQueryFilters()
			.Where(a => achievementIds.Contains(a.Id))
			.ToListAsync(TestContext.Current.CancellationToken);
		Assert.Contains(achievements, a => a.Name == "First Step");
	}

	[Fact]
	public async Task SeedAchievements_PopulatesData() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		await context.Database.ExecuteSqlRawAsync("truncate table \"public\".\"Achievement\" cascade",
			TestContext.Current.CancellationToken);
		await SeedAchievements.SeedAchievementsAsync(context);

		var count = await context.Achievements.IgnoreQueryFilters().CountAsync(TestContext.Current.CancellationToken);
		Assert.True(count > 20);
	}
}
