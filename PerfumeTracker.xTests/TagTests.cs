using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests;
public class TagTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = factory.Services.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
				if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
				var sql = "truncate table \"public\".\"Tag\" cascade";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Tags.AddRange(tagSeed);
				await context.SaveChangesAsync();
				dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	static List<Tag> tagSeed = new List<Tag> {
			new Tag { Id = Guid.NewGuid(), Color = "#FFFFFF", TagName = "Musky" },
			new Tag { Id = Guid.NewGuid(), Color = "#FF0000", TagName = "Woody" }
		};

	private async Task<Tag> GetFirst() {
		using var scope = factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		return await context.Tags.FirstAsync();
	}

	[Fact]
	public async Task GetTag() {
		await PrepareData();
		var tag = await GetFirst();
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/tags/{tag.Id}");
		response.EnsureSuccessStatusCode();

		var tags = await response.Content.ReadFromJsonAsync<TagDto>();
		Assert.NotNull(tags);
	}

	[Fact]
	public async Task GetTag_NotFound() {
		await PrepareData();
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/tags/{Guid.NewGuid()}");
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task GetTags() {
		await PrepareData();
		var client = factory.CreateClient();

		var response = await client.GetAsync("/api/tags");
		response.EnsureSuccessStatusCode();

		var tags = await response.Content.ReadFromJsonAsync<IEnumerable<TagDto>>();
		Assert.NotNull(tags);
		Assert.NotEmpty(tags);
	}

	[Fact]
	public async Task UpdateTag() {
		await PrepareData();
		var client = factory.CreateClient();
		var tag = await GetFirst();
		var dto = tag.Adapt<TagDto>();
		var content = JsonContent.Create(dto);
		var response = await client.PutAsync($"/api/tags/{dto.Id}", content);
		response.EnsureSuccessStatusCode();

		var tags = await response.Content.ReadFromJsonAsync<TagDto>();
		Assert.NotNull(tags);
	}

	[Fact]
	public async Task DeleteTag() {
		await PrepareData();
		var tag = await GetFirst();
		var client = factory.CreateClient();
		var response = await client.DeleteAsync($"/api/tags/{tag.Id}");
		response.EnsureSuccessStatusCode();
		Assert.True(true);
	}

	[Fact]
	public async Task AddTag() {
		await PrepareData();
		var client = factory.CreateClient();
		var dto = new TagDto("Purple", "#630330", Guid.NewGuid());
		var content = JsonContent.Create(dto);
		var response = await client.PostAsync($"/api/tags", content);
		response.EnsureSuccessStatusCode();

		var tags = await response.Content.ReadFromJsonAsync<TagDto>();
		Assert.NotNull(tags);
	}

	[Fact]
	public async Task GetStats() {
		await PrepareData();
		var client = factory.CreateClient();

		var response = await client.GetAsync("/api/tags/stats");
		response.EnsureSuccessStatusCode();

		var tags = await response.Content.ReadFromJsonAsync<IEnumerable<TagStatDto>>();
		Assert.NotNull(tags);
	}
}
