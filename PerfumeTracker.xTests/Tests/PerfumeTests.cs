using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.PerfumeRatings;
using PerfumeTracker.Server.Features.PerfumeRatings.Services;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Perfume Tests")]
public class PerfumeCollection : ICollectionFixture<PerfumeFixture>;

public class PerfumeFixture : DbFixture {
	public PerfumeFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var tags = await SeedTags(3);
		var perfumes = await SeedPerfumes(3);

		// Generate perfume tags (linking perfumes to tags)
		var perfumeIds = perfumes.Select(p => p.Id).ToList();
		var tagIds = tags.Select(t => t.Id).ToList();
		var perfumeTags = GeneratePerfumeTags(perfumeIds, tagIds);
		await context.PerfumeTags.AddRangeAsync(perfumeTags);
		await context.SaveChangesAsync();
	}
}

[Collection("Perfume Tests")]
public class PerfumeTests {

	private readonly PerfumeFixture _fixture;
	public PerfumeTests(PerfumeFixture fixture) {
		_fixture = fixture;
	}


	[Fact]
	public async Task GetPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();

		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var handler = new GetPerfumeHandler(context, new MockPresignedUrlService(), userProfileService);
		var response = await handler.Handle(new GetPerfumeQuery(perfume.Id), TestContext.Current.CancellationToken);
		Assert.NotNull(response);
	}

	[Fact]
	public async Task GetPerfume_NotFound() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();

		var handler = new GetPerfumeHandler(context, new MockPresignedUrlService(), userProfileService);
		await Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(new GetPerfumeQuery(Guid.NewGuid()), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task GetPerfumes() {
		// Arrange
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var handler = new GetPerfumesWithWornHandler(context, new MockPresignedUrlService(), userProfileService);

		// Act
		var perfumes = await handler.Handle(new GetPerfumesWithWornQuery(), TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task UpdatePerfume() {
		// Arrange
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new UpdatePerfumeHandler(context, _fixture.MockSideEffectQueue.Object);
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var tag = await context.Tags.FirstAsync(TestContext.Current.CancellationToken);
		var dto = new PerfumeUploadDto(House: perfume.House,
			PerfumeName: perfume.PerfumeName,
			Family: perfume.Family,
			perfume.Ml,
			perfume.MlLeft,
			new List<TagDto>() { tag.Adapt<TagDto>() });

		// Act
		var response = await handler.Handle(new UpdatePerfumeCommand(perfume.Id, dto), TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(response);
	}

	[Fact]
	public async Task DeletePerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new DeletePerfumeHandler(context);
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var result = await handler.Handle(new DeletePerfumeCommand(perfume.Id), TestContext.Current.CancellationToken);
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfumes = _fixture.GeneratePerfumes(1);
		var dto = perfumes[0].Adapt<PerfumeUploadDto>();
		var handler = new AddPerfumeHandler(context, _fixture.MockSideEffectQueue.Object);
		var response = await handler.Handle(new AddPerfumeCommand(dto), TestContext.Current.CancellationToken);
		Assert.NotNull(response);
	}

	[Fact]
	public async Task GetFullText() {
		// Arrange
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var handler = new GetPerfumesWithWornHandler(context, new MockPresignedUrlService(), userProfileService);
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);

		// Act
		var response = await handler.Handle(new GetPerfumesWithWornQuery(perfume.PerfumeName), TestContext.Current.CancellationToken);

		// Assert
		Assert.NotNull(response);
		Assert.NotEmpty(response);
	}

	[Fact]
	public async Task AddPerfumeRating() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var dto = new PerfumeRatingUploadDto(perfume.Id, 5, "Nice perfume!");
		var handler = new AddPerfumeRatingHandler(ratingService);
		var response = await handler.Handle(new AddPerfumeRatingCommand(dto), TestContext.Current.CancellationToken);
		Assert.NotNull(response);
	}

	[Fact]
	public async Task DeletePerfumeRating() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var ratingService = scope.ServiceProvider.GetRequiredService<IRatingService>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var dto = new PerfumeRatingUploadDto(perfume.Id, 5, "Nice perfume!");
		var handler = new AddPerfumeRatingHandler(ratingService);
		var rating = await handler.Handle(new AddPerfumeRatingCommand(dto), TestContext.Current.CancellationToken);

		var deleteHandler = new DeletePerfumeRatingHandler(ratingService);
		rating = await deleteHandler.Handle(new DeletePerfumeRatingCommand(rating.PerfumeId, rating.Id), TestContext.Current.CancellationToken);
		Assert.NotNull(rating);
		Assert.True(rating.IsDeleted);
	}

	[Fact]
	public async Task GetNextPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var handler = new GetNextPerfumeHandler(context, userProfileService);
		var response = await handler.Handle(new GetNextPerfumeIdQuery(perfume.Id), TestContext.Current.CancellationToken);
		Assert.NotEqual(perfume.Id, response); // Assuming there is more than one perfume, the next perfume ID should be different
	}
	[Fact]
	public async Task GetPreviousPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync(TestContext.Current.CancellationToken);
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var handler = new GetPreviousPerfumeHandler(context, userProfileService);
		var response = await handler.Handle(new GetPreviousPerfumeIdQuery(perfume.Id), TestContext.Current.CancellationToken);
		Assert.NotEqual(perfume.Id, response); // Assuming there is more than one perfume, the previous perfume ID should be different
	}
}
