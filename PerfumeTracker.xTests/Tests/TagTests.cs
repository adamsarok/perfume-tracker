using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Tags;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Tag Tests")]
public class TagCollection : ICollectionFixture<TagFixture>;

public class TagFixture : DbFixture {
	public TagFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var sql = "truncate table \"public\".\"Tag\" cascade";
		await context.Database.ExecuteSqlRawAsync(sql);

		var tags = GenerateTags(2);
		await context.Tags.AddRangeAsync(tags);
		await context.SaveChangesAsync();
	}
}

[Collection("Tag Tests")]
public class TagTests {
	private readonly TagFixture _fixture;

	public TagTests(TagFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task GetTag() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var getTagHandler = new GetTagHandler(context);
		var tag = await context.Tags.FirstAsync(TestContext.Current.CancellationToken);
		var result = await getTagHandler.Handle(new GetTagQuery(tag.Id), TestContext.Current.CancellationToken);
		Assert.NotNull(result);
		Assert.Equal(tag.Id, result.Id);
	}

	[Fact]
	public async Task GetTag_NotFound() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var getTagHandler = new GetTagHandler(context);
		_ = await Assert.ThrowsAsync<NotFoundException>(async () =>
			await getTagHandler.Handle(new GetTagQuery(Guid.NewGuid()), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task GetTags() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var getTagsHandler = new GetTagsHandler(context);
		var tags = await getTagsHandler.Handle(new GetTagsQuery(), TestContext.Current.CancellationToken);
		Assert.NotNull(tags);
		Assert.NotEmpty(tags);
	}

	[Fact]
	public async Task UpdateTag() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var tag = await context.Tags.FirstAsync(TestContext.Current.CancellationToken);
		tag.TagName = Guid.NewGuid().ToString();
		var dto = tag.Adapt<TagUploadDto>();
		var updateTagHandler = new UpdateTagHandler(context);
		var tagResult = await updateTagHandler.Handle(new UpdateTagCommand(tag.Id, dto), TestContext.Current.CancellationToken);
		Assert.Equal(tag.TagName, tagResult.TagName);
	}

	[Fact]
	public async Task DeleteTag() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var tag = await context.Tags.FirstAsync(TestContext.Current.CancellationToken);
		var deleteTagHandler = new DeleteTagHandler(context);
		var result = await deleteTagHandler.Handle(new DeleteTagCommand(tag.Id), TestContext.Current.CancellationToken);
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddTag() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var dto = new TagUploadDto("Purple", "#630330", "Purple Scent");
		var addTagHandler = new AddTagHandler(context);
		var result = await addTagHandler.Handle(new AddTagCommand(dto), TestContext.Current.CancellationToken);
		Assert.NotNull(await context.Tags.FindAsync(new object[] { result.Id }, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task GetStats() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var getTagStatsHandler = new GetTagStatsHandler(context);
		var tags = await getTagStatsHandler.Handle(new GetTagStatsQuery(), TestContext.Current.CancellationToken);
		Assert.NotNull(tags);
		Assert.NotEmpty(tags);
	}
}
