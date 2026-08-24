using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.YearInReview;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("PerfumeWorn Tests")]
public class PerfumeWornCollection : ICollectionFixture<PerfumeWornFixture>;

public class PerfumeWornFixture : DbFixture {
	public PerfumeWornFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var sql = "truncate table \"public\".\"YearInReviewSnapshot\" cascade; truncate table \"public\".\"PerfumeEvent\" cascade; truncate table \"public\".\"Perfume\" cascade;";
		await context.Database.ExecuteSqlRawAsync(sql);

		var perfumes = GeneratePerfumes(3);
		await context.Perfumes.AddRangeAsync(perfumes);
		await context.SaveChangesAsync();
		var previousYear = DateTime.UtcNow.Year - 1;

		var events = GeneratePerfumeEvents(1, perfumes[0].Id);
		events[0].Type = PerfumeEvent.PerfumeEventType.Worn;
		events[0].EventDate = new DateTime(previousYear, 6, 15, 0, 0, 0, DateTimeKind.Utc);
		await context.PerfumeEvents.AddRangeAsync(events);

		var events2 = GeneratePerfumeEvents(1, perfumes[1].Id);
		events2[0].Type = PerfumeEvent.PerfumeEventType.Worn;
		events2[0].EventDate = new DateTime(previousYear, 6, 14, 0, 0, 0, DateTimeKind.Utc);
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
		var result = await handler.Handle(new GetWornPerfumesQuery(0, 20), TestContext.Current.CancellationToken);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	public async Task GetYearInReviewAggregatesWearsOnServer() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new GetYearInReviewHandler(context, new MockPresignedUrlService());

		var result = await handler.Handle(
			new GetYearInReviewQuery(),
			TestContext.Current.CancellationToken);

		Assert.Equal(DateTime.UtcNow.Year - 1, result.Year);
		Assert.NotNull(result.BusiestMonth);
		Assert.NotNull(result.BusiestDay);
		var snapshotPayload = await context.YearInReviewSnapshots
			.Select(x => x.Payload)
			.SingleAsync(TestContext.Current.CancellationToken);
		Assert.DoesNotContain("http://test.invalid", snapshotPayload, StringComparison.Ordinal);

		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		context.PerfumeEvents.Add(new PerfumeEvent {
			PerfumeId = perfume.Id,
			EventDate = new DateTime(DateTime.UtcNow.Year - 1, 7, 1, 0, 0, 0, DateTimeKind.Utc),
			Type = PerfumeEvent.PerfumeEventType.Worn
		});
		await context.SaveChangesAsync(TestContext.Current.CancellationToken);

		var persistedResult = await handler.Handle(
			new GetYearInReviewQuery(),
			TestContext.Current.CancellationToken);
		Assert.Equal(2, persistedResult.TotalWears);
		Assert.All(persistedResult.TopPerfumes, item => Assert.Equal("http://test.invalid/", item.ImageUrl));
	}

	[Fact]
	public async Task DeletePerfumeWorn() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var worn = await context.PerfumeEvents.FirstAsync(TestContext.Current.CancellationToken);
		var handler = new DeletePerfumeEventHandler(context);
		var result = await handler.Handle(new DeletePerfumeEventCommand(worn.Id), TestContext.Current.CancellationToken);
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddPerfumeWorn() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();

		var perfume = await context.Perfumes.Skip(2).FirstAsync(TestContext.Current.CancellationToken);
		var dto = new PerfumeEventUploadDto(perfume.Id, DateTime.UtcNow, PerfumeEvent.PerfumeEventType.Worn, 0.05m, Guid.NewGuid());
		var handler = new AddPerfumeEventHandler(context, _fixture.MockSideEffectQueue.Object, userProfileService);
		var result = await handler.Handle(new AddPerfumeEventCommand(dto), TestContext.Current.CancellationToken);
		Assert.True(await context.PerfumeEvents.AnyAsync(x => x.Id == result.Id, TestContext.Current.CancellationToken));
	}
}
