using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.PerfumeRandoms;
using PerfumeTracker.xTests.Fixture;

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

		var handler = new GetRandomPerfumeHandler(context, _fixture.MockSideEffectQueue.Object);
		var response = await handler.Handle(new GetRandomPerfumeQuery(), CancellationToken.None);
		Assert.NotNull(response);
	}
}
