using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("WornPerfumeIds Tests")]
public class WornPerfumeIdsCollection : ICollectionFixture<WornPerfumeIdsFixture>;

public class WornPerfumeIdsFixture : DbFixture {
	public WornPerfumeIdsFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var perfumes = await SeedPerfumes(2);
		// Create worn events within last 7 days for first perfume
		var recentEvents = PerfumeEventFaker.Clone()
			.RuleFor(pe => pe.PerfumeId, perfumes[0].Id)
			.RuleFor(pe => pe.UserId, TenantProvider.MockTenantId!.Value)
			.RuleFor(pe => pe.Type, PerfumeEvent.PerfumeEventType.Worn)
			.RuleFor(pe => pe.EventDate, f => DateTime.UtcNow.AddDays(-2))
			.Generate(2);
		await context.PerfumeEvents.AddRangeAsync(recentEvents);

		// Create worn events 60 days ago for second perfume
		var oldEvents = PerfumeEventFaker.Clone()
			.RuleFor(pe => pe.PerfumeId, perfumes[1].Id)
			.RuleFor(pe => pe.UserId, TenantProvider.MockTenantId!.Value)
			.RuleFor(pe => pe.Type, PerfumeEvent.PerfumeEventType.Worn)
			.RuleFor(pe => pe.EventDate, f => DateTime.UtcNow.AddDays(-60))
			.Generate(2);
		await context.PerfumeEvents.AddRangeAsync(oldEvents);
		await context.SaveChangesAsync();
	}
}

[Collection("WornPerfumeIds Tests")]
public class WornPerfumeIdsTests {
	private readonly WornPerfumeIdsFixture _fixture;
	public WornPerfumeIdsTests(WornPerfumeIdsFixture fixture) => _fixture = fixture;

	[Fact]
	public async Task GetWornPerfumeIds_FiltersByDays_ReturnsRecentOnly() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new GetWornPerfumeIdsHandler(context);

		var result = await handler.Handle(
			new GetWornPerfumeIdsQuery(7),
			TestContext.Current.CancellationToken);

		Assert.Single(result); // Only the first perfume has events within 7 days
	}

	[Fact]
	public async Task GetWornPerfumeIds_LargerFilter_ReturnsBoth() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new GetWornPerfumeIdsHandler(context);

		var result = await handler.Handle(
			new GetWornPerfumeIdsQuery(90),
			TestContext.Current.CancellationToken);

		Assert.Equal(2, result.Count); // Both perfumes should be within 90 days
	}
}
