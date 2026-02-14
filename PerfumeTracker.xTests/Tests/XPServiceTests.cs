using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Common.Services;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("XP Tests")]
public class XPCollection : ICollectionFixture<XPFixture>;

public class XPFixture : DbFixture {
	public XPFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var missions = await SeedMissions(2);
		// Create completed user missions with XP
		var userMissions = UserMissionFaker.Clone()
			.RuleFor(um => um.UserId, TenantProvider.MockTenantId!.Value)
			.RuleFor(um => um.MissionId, f => f.PickRandom(missions).Id)
			.RuleFor(um => um.IsCompleted, true)
			.RuleFor(um => um.CompletedAt, DateTime.UtcNow)
			.RuleFor(um => um.XP_Awarded, 150)
			.Generate(3);
		await context.UserMissions.AddRangeAsync(userMissions);

		// Seed an active streak
		var streaks = UserStreakFaker.Clone()
			.RuleFor(us => us.UserId, TenantProvider.MockTenantId!.Value)
			.RuleFor(us => us.StreakEndAt, f => (DateTime?)null)
			.RuleFor(us => us.Progress, 5)
			.Generate(1);
		await context.UserStreaks.AddRangeAsync(streaks);
		await context.SaveChangesAsync();
	}
}

[Collection("XP Tests")]
public class XPServiceTests {
	private readonly XPFixture _fixture;
	public XPServiceTests(XPFixture fixture) => _fixture = fixture;

	[Fact]
	public async Task GetUserXP_ReturnsXPFromCompletedMissions() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var service = new XPService(context);
		var userId = _fixture.TenantProvider.MockTenantId!.Value;

		var result = await service.GetUserXP(userId, TestContext.Current.CancellationToken);

		Assert.Equal(userId, result.UserId);
		Assert.True(result.Xp > 0);
		Assert.True(result.Level >= 1);
	}

	[Fact]
	public async Task GetUserStreak_WithActiveStreak_ReturnsStreak() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var service = new XPService(context);
		var userId = _fixture.TenantProvider.MockTenantId!.Value;

		var result = await service.GetUserStreak(userId, TestContext.Current.CancellationToken);

		Assert.True(result.StreakDays > 0);
		Assert.True(result.XpMultiplier > 1m);
	}

	[Fact]
	public void GetXPMultiplier_VariousStreakDays() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var service = new XPService(context);

		Assert.Equal(1m, service.GetXPMultiplier(0));
		Assert.Equal(1.15m, service.GetXPMultiplier(5));
		Assert.Equal(1.3m, service.GetXPMultiplier(10));
		// 30 days: 1.0 + 0.3 + (30-10)*0.02 = 1.7
		Assert.Equal(1.7m, service.GetXPMultiplier(30));
		// Cap at 3.0
		Assert.Equal(XPService.MaxMultiplier, service.GetXPMultiplier(200));
	}
}

public class LevelsTests {
	[Fact]
	public void GetLevels_ReturnsCorrectCount() {
		var levels = Levels.GetLevels();
		Assert.Equal(13, levels.Count);
	}

	[Fact]
	public void GetLevels_FirstLevelStartsAtZero() {
		var levels = Levels.GetLevels();
		Assert.Equal(0, levels[0].MinXP);
		Assert.Equal(1, levels[0].LevelNum);
	}

	[Fact]
	public void GetLevels_LevelsAreContiguous() {
		var levels = Levels.GetLevels();
		for (int i = 1; i < levels.Count; i++) {
			Assert.Equal(levels[i - 1].MaxXP + 1, levels[i].MinXP);
		}
	}
}
