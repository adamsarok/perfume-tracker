using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.PerfumeRatings;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Perfume Tests")]
public class PerfumeCollection : ICollectionFixture<DbFixture>;

[Collection("Perfume Tests")]
public class PerfumeTests {

	private readonly DbFixture _fixture;
	public PerfumeTests(DbFixture fixture) {
		_fixture = fixture;
	}

	/* TODO:
			scope.PerfumeTrackerContext.Tags.AddRange(tagSeed);
				scope.PerfumeTrackerContext.Perfumes.AddRange(perfumeSeed);
				scope.PerfumeTrackerContext.PerfumeTags.AddRange(perfumeTagSeed); 
	*/

	[Fact]
	public async Task GetPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var perfume = await context.Perfumes.FirstAsync();
		var handler = new GetPerfumeHandler(context, new MockPresignedUrlService());
		var response = await handler.Handle(new GetPerfumeQuery(perfume.Id), CancellationToken.None);
		Assert.NotNull(response);
	}

	[Fact]
	public async Task GetPerfume_NotFound() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var handler = new GetPerfumeHandler(context, new MockPresignedUrlService());
		await Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(new GetPerfumeQuery(Guid.NewGuid()), CancellationToken.None));
	}

	[Fact]
	public async Task GetPerfumes() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new GetPerfumesWithWornHandler(context, new MockPresignedUrlService());
		var perfumes = await handler.Handle(new GetPerfumesWithWornQuery(), CancellationToken.None);
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task UpdatePerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new UpdatePerfumeHandler(context, _fixture.MockSideEffectQueue.Object);
		var perfume = await context.Perfumes.FirstAsync();
		var tag = await context.Tags.FirstAsync();
		var dto = new PerfumeUploadDto(perfume.House,
			perfume.PerfumeName,
			perfume.Ml,
			perfume.MlLeft,
			perfume.Autumn,
			perfume.Spring,
			perfume.Summer,
			perfume.Winter,
			new List<TagDto>() { tag.Adapt<TagDto>() });
		var response = await handler.Handle(new UpdatePerfumeCommand(perfume.Id, dto), CancellationToken.None);
		Assert.NotNull(response);
	}

	[Fact]
	public async Task DeletePerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new DeletePerfumeHandler(context);
		var perfume = await context.Perfumes.FirstAsync();
		var result = await handler.Handle(new DeletePerfumeCommand(perfume, CancellationToken.None);
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var dto = new PerfumeUploadDto("House3", "Perfume3", 50, 50, true, true, false, false, new());
		var handler = new AddPerfumeHandler(context, _fixture.MockSideEffectQueue.Object);
		var response = await handler.Handle(new AddPerfumeCommand(dto), CancellationToken.None);
		Assert.NotNull(response);
	}

	[Fact]
	public async Task GetFullText() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var handler = new GetPerfumesWithWornHandler(context, new MockPresignedUrlService());
		var perfume = await context.Perfumes.FirstAsync();
		var response = await handler.Handle(new GetPerfumesWithWornQuery(perfume.PerfumeName), CancellationToken.None);
		Assert.NotNull(response);
		Assert.NotEmpty(response);
	}

	[Fact]
	public async Task AddPerfumeRating() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync();
		var dto = new PerfumeRatingUploadDto(perfume.Id, 5, "Nice perfume!");
		var handler = new AddPerfumeRatingHandler(context);
		var response = await handler.Handle(new AddPerfumeRatingCommand(dto), CancellationToken.None);
		Assert.NotNull(response);
	}

	[Fact]
	public async Task DeletePerfumeRating() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync();
		var dto = new PerfumeRatingUploadDto(perfume.Id, 5, "Nice perfume!");
		var handler = new AddPerfumeRatingHandler(context);
		var rating = await handler.Handle(new AddPerfumeRatingCommand(dto), CancellationToken.None);

		var deleteHandler = new DeletePerfumeRatingHandler(context);
		rating = await deleteHandler.Handle(new DeletePerfumeRatingCommand(rating.PerfumeId, rating.Id), CancellationToken.None);
		Assert.NotNull(rating);
		Assert.True(rating.IsDeleted);
	}

	[Fact]
	public async Task GetNextPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync();
		var handler = new GetNextPerfumeHandler(context);
		var response = await handler.Handle(new GetNextPerfumeIdQuery(perfume.Id), CancellationToken.None);
		Assert.Equal(perfume.Id, response);
	}
	[Fact]
	public async Task GetPreviousPerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfume = await context.Perfumes.FirstAsync();
		var handler = new GetPreviousPerfumeHandler(context);
		var response = await handler.Handle(new GetPreviousPerfumeIdQuery(perfume.Id), CancellationToken.None);
		Assert.Equal(perfume.Id, response);
	}
}
