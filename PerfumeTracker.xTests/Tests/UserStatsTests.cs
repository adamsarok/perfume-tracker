using Microsoft.Extensions.DependencyInjection;
using Moq;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Features.Users.Services;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("UserStats Tests")]
public class UserStatsCollection : ICollectionFixture<UserStatsFixture>;

public class UserStatsFixture : DbFixture {
	public UserStatsFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var perfumes = await SeedPerfumes(3);
		// Set ratings and wear counts on perfumes
		foreach (var p in perfumes) {
			var dbPerfume = await context.Perfumes.FindAsync(p.Id);
			if (dbPerfume != null) {
				dbPerfume.AverageRating = 8.0m;
				dbPerfume.WearCount = 5;
			}
		}
		await context.SaveChangesAsync();

		var events = GeneratePerfumeEvents(5, perfumes[0].Id);
		foreach (var e in events) e.Type = PerfumeEvent.PerfumeEventType.Worn;
		await context.PerfumeEvents.AddRangeAsync(events);
		await context.SaveChangesAsync();

		var streaks = UserStreakFaker.Clone()
			.RuleFor(us => us.UserId, TenantProvider.MockTenantId!.Value)
			.RuleFor(us => us.StreakEndAt, f => (DateTime?)null)
			.RuleFor(us => us.Progress, 7)
			.Generate(1);
		await context.UserStreaks.AddRangeAsync(streaks);
		await context.SaveChangesAsync();
	}
}

[Collection("UserStats Tests")]
public class UserStatsTests {
	private readonly UserStatsFixture _fixture;
	public UserStatsTests(UserStatsFixture fixture) => _fixture = fixture;

	[Fact]
	public async Task GetUserStats_ReturnsStats() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var mockRecommender = new Mock<IPerfumeRecommender>();
		mockRecommender.Setup(r => r.GetRecommendationStats(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Enumerable.Empty<PerfumeRecommendationStats>());
		var service = new UserStatsService(context, mockRecommender.Object);

		var stats = await service.GetUserStats(TestContext.Current.CancellationToken);

		Assert.NotNull(stats);
		Assert.True(stats.WearCount > 0);
		Assert.True(stats.PerfumesCount > 0);
	}

	[Fact]
	public async Task GetUserStats_HasFavoritePerfumes() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var mockRecommender = new Mock<IPerfumeRecommender>();
		mockRecommender.Setup(r => r.GetRecommendationStats(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Enumerable.Empty<PerfumeRecommendationStats>());
		var service = new UserStatsService(context, mockRecommender.Object);

		var stats = await service.GetUserStats(TestContext.Current.CancellationToken);

		Assert.NotEmpty(stats.FavoritePerfumes);
	}

	[Fact]
	public async Task GetUserStats_HasStreakInfo() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var mockRecommender = new Mock<IPerfumeRecommender>();
		mockRecommender.Setup(r => r.GetRecommendationStats(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Enumerable.Empty<PerfumeRecommendationStats>());
		var service = new UserStatsService(context, mockRecommender.Object);

		var stats = await service.GetUserStats(TestContext.Current.CancellationToken);

		Assert.NotNull(stats.CurrentStreak);
		Assert.NotNull(stats.BestStreak);
	}
}
