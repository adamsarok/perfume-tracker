using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.PerfumeRatings;
using PerfumeTracker.Server.Features.PerfumeRatings.Services;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("PerfumeRating Tests")]
public class PerfumeRatingCollection : ICollectionFixture<PerfumeRatingFixture>;

public class PerfumeRatingFixture : DbFixture {
	public PerfumeRatingFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var perfumes = await SeedPerfumes(2);
		// Seed some ratings for the first perfume
		var ratings = GeneratePerfumeRatings(3, perfumes[0].Id);
		await context.PerfumeRatings.AddRangeAsync(ratings);
		await context.SaveChangesAsync();
	}
}

[Collection("PerfumeRating Tests")]
public class PerfumeRatingTests {
	private readonly PerfumeRatingFixture _fixture;
	public PerfumeRatingTests(PerfumeRatingFixture fixture) => _fixture = fixture;

	[Fact]
	public async Task GetPerfumeRatings_ReturnsRatingsForPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var handler = new GetPerfumeRatingHandler(context);

		var ratings = await handler.Handle(
			new GetPerfumeRatingQuery(perfume.Id),
			TestContext.Current.CancellationToken);

		Assert.NotNull(ratings);
		Assert.True(ratings.Count() > 3);
	}

	[Fact]
	public async Task AddPerfumeRating_CreatesRatingAndUpdatesAverage() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);

		var result = await ratingService.AddPerfumeRating(
			perfume.Id, 9.0m, "Excellent!", TestContext.Current.CancellationToken);

		Assert.Equal(perfume.Id, result.PerfumeId);
		Assert.Equal(9.0m, result.Rating);

		// Verify average was updated
		var updatedPerfume = await context.Perfumes.FindAsync([perfume.Id], TestContext.Current.CancellationToken);
		Assert.NotNull(updatedPerfume);
		Assert.True(updatedPerfume.AverageRating > 0,
			$"Expected positive average rating, got {updatedPerfume.AverageRating}");
	}

	[Fact]
	public async Task DeletePerfumeRating_SoftDeletes() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);

		var remain = await ratingService.AddPerfumeRating(
			perfume.Id, 10.0m, "Good", TestContext.Current.CancellationToken);

		// Add a rating first
		var added = await ratingService.AddPerfumeRating(
			perfume.Id, 7.0m, "Good", TestContext.Current.CancellationToken);

		// Delete it
		var deleted = await ratingService.DeletePerfumeRating(
			added.Id, TestContext.Current.CancellationToken);

		Assert.True(deleted.IsDeleted);
	}
}
