using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("PerfumeWorn Tests")]
public class PerfumeWornCollection : ICollectionFixture<PerfumeWornFixture>;

public class PerfumeWornFixture : DbFixture {
	public PerfumeWornFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var sql = "truncate table \"public\".\"PerfumeEvent\" cascade; truncate table \"public\".\"Perfume\" cascade;";
		await context.Database.ExecuteSqlRawAsync(sql);

		var perfumes = GeneratePerfumes(3);
		await context.Perfumes.AddRangeAsync(perfumes);
		await context.SaveChangesAsync();

		var events = GeneratePerfumeEvents(1, perfumes[0].Id);
		events[0].Type = PerfumeEvent.PerfumeEventType.Worn;
		events[0].EventDate = DateTime.UtcNow;
		await context.PerfumeEvents.AddRangeAsync(events);

		var events2 = GeneratePerfumeEvents(1, perfumes[1].Id);
		events2[0].Type = PerfumeEvent.PerfumeEventType.Worn;
		events2[0].EventDate = DateTime.UtcNow.AddDays(-1);
		await context.PerfumeEvents.AddRangeAsync(events2);

		await context.SaveChangesAsync();
	}
}

[Collection("PerfumeWorn Tests")]
public class PerfumeWornTests {
	private readonly PerfumeWornFixture _fixture;

	public PerfumeWornTests(PerfumeWornFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task GetPerfumeWorns() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var handler = new GetWornPerfumesHandler(context, new MockPresignedUrlService());
		var result = await handler.Handle(new GetWornPerfumesQuery(0, 20), CancellationToken.None);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	public async Task DeletePerfumeWorn() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var worn = await context.PerfumeEvents.FirstAsync();
		var handler = new DeletePerfumeEventHandler(context);
		var result = await handler.Handle(new DeletePerfumeEventCommand(worn.Id), CancellationToken.None);
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddPerfumeWorn() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();

		var perfume = await context.Perfumes.Skip(2).FirstAsync();
		var dto = new PerfumeEventUploadDto(perfume.Id, DateTime.UtcNow, PerfumeEvent.PerfumeEventType.Worn, 0.05m, Guid.NewGuid());
		var handler = new AddPerfumeEventHandler(context, _fixture.MockSideEffectQueue.Object, userProfileService);
		var result = await handler.Handle(new AddPerfumeEventCommand(dto), CancellationToken.None);
		Assert.True(await context.PerfumeEvents.AnyAsync(x => x.Id == result.Id));
	}
}
