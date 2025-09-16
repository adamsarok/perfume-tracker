using Microsoft.AspNetCore.Mvc.Testing;
using PerfumeTracker.Server.Features.Tags;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;
public class TagTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public TagTests(WebApplicationFactory<Program> factory) : base(factory) { }
	private static readonly SemaphoreSlim semaphore = new(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!DbUp) {
				using var scope = GetTestScope();
				var sql = "truncate table \"public\".\"Tag\" cascade";
				_ = await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync(sql);
				scope.PerfumeTrackerContext.Tags.AddRange(tagSeed);
				_ = await scope.PerfumeTrackerContext.SaveChangesAsync();
				DbUp = true;
			}
		} finally {
			_ = semaphore.Release();
		}
	}

	static readonly List<Tag> tagSeed = [
		new Tag { Id = Guid.NewGuid(), Color = "#FFFFFF", TagName = "Musky" },
		new Tag { Id = Guid.NewGuid(), Color = "#FF0000", TagName = "Woody" }
	];

	[Fact]
	public async Task GetTag() {
		await PrepareData();
		using var scope = GetTestScope();
		var getTagHandler = new GetTagHandler(scope.PerfumeTrackerContext);
		var tag = await scope.PerfumeTrackerContext.Tags.FirstAsync();
		var result = await getTagHandler.Handle(new GetTagQuery(tag.Id), CancellationToken.None);
		Assert.NotNull(result);
		Assert.Equal(tag.Id, result.Id);
	}

	[Fact]
	public async Task GetTag_NotFound() {
		await PrepareData();
		using var scope = GetTestScope();
		var getTagHandler = new GetTagHandler(scope.PerfumeTrackerContext);
		_ = await Assert.ThrowsAsync<NotFoundException>(async () =>
			await getTagHandler.Handle(new GetTagQuery(Guid.NewGuid()), CancellationToken.None));
	}

	[Fact]
	public async Task GetTags() {
		await PrepareData();
		using var scope = GetTestScope();
		var getTagsHandler = new GetTagsHandler(scope.PerfumeTrackerContext);
		var tags = await getTagsHandler.Handle(new GetTagsQuery(), CancellationToken.None);
		Assert.NotNull(tags);
		Assert.NotEmpty(tags);
	}

	[Fact]
	public async Task UpdateTag() {
		await PrepareData();
		using var scope = GetTestScope();
		var tag = await scope.PerfumeTrackerContext.Tags.FirstAsync();
		tag.TagName = Guid.NewGuid().ToString();
		var dto = tag.Adapt<TagUploadDto>();
		var updateTagHandler = new UpdateTagHandler(scope.PerfumeTrackerContext);
		var tagResult = await updateTagHandler.Handle(new UpdateTagCommand(tag.Id, dto), CancellationToken.None);
		Assert.Equal(tag.TagName, tagResult.TagName);
	}

	[Fact]
	public async Task DeleteTag() {
		await PrepareData();
		using var scope = GetTestScope();
		var tag = await scope.PerfumeTrackerContext.Tags.FirstAsync();
		var deleteTagHandler = new DeleteTagHandler(scope.PerfumeTrackerContext);
		var result = await deleteTagHandler.Handle(new DeleteTagCommand(tag.Id), CancellationToken.None);
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddTag() {
		await PrepareData();
		using var scope = GetTestScope();
		var dto = new TagUploadDto("Purple", "#630330");
		var addTagHandler = new AddTagHandler(scope.PerfumeTrackerContext);
		var result = await addTagHandler.Handle(new AddTagCommand(dto), CancellationToken.None);
		Assert.NotNull(await scope.PerfumeTrackerContext.Tags.FindAsync(result.Id));
	}

	[Fact]
	public async Task GetStats() {
		await PrepareData();
		using var scope = GetTestScope();
		var getTagStatsHandler = new GetTagStatsHandler(scope.PerfumeTrackerContext);
		var tags = await getTagStatsHandler.Handle(new GetTagStatsQuery(), CancellationToken.None);
		Assert.NotNull(tags);
		Assert.NotEmpty(tags);
	}
}
