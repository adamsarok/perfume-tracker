using Microsoft.Extensions.DependencyInjection;
using Moq;
using PerfumeTracker.Server.Features.PerfumeRandoms;
using PerfumeTracker.Server.Features.Perfumes.Extensions;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Common;
using PerfumeTracker.xTests.Fixture;
using static PerfumeTracker.Server.Features.Perfumes.GetPerfumeRecommendations;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("RandomPerfume Tests")]
public class RandomPerfumeCollection : ICollectionFixture<RandomPerfumeFixture>;

public class RandomPerfumeFixture : DbFixture {
	public RandomPerfumeFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var sql = "truncate table \"public\".\"PerfumeEvent\" cascade; truncate table \"public\".\"Perfume\" cascade;  truncate table \"public\".\"PerfumeRandom\" cascade;";
		await context.Database.ExecuteSqlRawAsync(sql);

		var perfumes = GeneratePerfumes(3);
		await context.Perfumes.AddRangeAsync(perfumes);
		await context.SaveChangesAsync();

		var events = GeneratePerfumeEvents(1, perfumes[0].Id);
		events[0].Type = PerfumeEvent.PerfumeEventType.Worn;
		events[0].EventDate = DateTime.UtcNow;
		await context.PerfumeEvents.AddRangeAsync(events);

		var randoms = await SeedPerfumeRandoms(3);

		await context.SaveChangesAsync();
	}
}

[Collection("RandomPerfume Tests")]
public class RandomPerfumeTests {
	private readonly RandomPerfumeFixture _fixture;

	public RandomPerfumeTests(RandomPerfumeFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task GetPerfumeSuggestion() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var profileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var userProfile = await profileService.GetCurrentUserProfile(CancellationToken.None);
		var presignedUrlService = scope.ServiceProvider.GetRequiredService<IPresignedUrlService>();
		var perfume = await context.Perfumes.FirstAsync();
		var mockRecommender = new Mock<IPerfumeRecommender>();
		_ = mockRecommender.Setup(x =>
			x.GetRecommendationsForStrategy(It.IsAny<RecommendationStrategy>(), 1, It.IsAny<CancellationToken>()))
			.ReturnsAsync(new List<PerfumeRecommendationDto>() {
				new PerfumeRecommendationDto(perfume.ToPerfumeWithWornStatsDto(userProfile, presignedUrlService), RecommendationStrategy.Random)
			});
		var handler = new GetRandomPerfumeHandler(context, _fixture.MockSideEffectQueue.Object, mockRecommender.Object);
		var response = await handler.Handle(new GetRandomPerfumeQuery(), CancellationToken.None);
		Assert.NotNull(response);
	}
}
