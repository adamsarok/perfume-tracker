using Mapster;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Dto;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;
using System.Net;
using System.Net.Http.Json;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace PerfumeTracker.xTests;

public class PerfumeTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = factory.Services.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
				var sql = "truncate table \"public\".\"Perfume\" cascade";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Perfumes.AddRange(perfumeSeed);
				await context.SaveChangesAsync();
				dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	static List<Perfume> perfumeSeed = new List<Perfume> {
			new Perfume { Id = 0, House = "House1", PerfumeName = "Perfume1", Rating = 10
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("Perfume1")
			},
			new Perfume { Id = 0, House = "House2", PerfumeName = "Perfume2", Rating = 1
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("Perfume2")
			},
		};

	private async Task<Perfume> GetFirst() {
		using var scope = factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
		return await context.Perfumes.FirstAsync();
	}

	[Fact]
	public async Task GetPerfume() {
		await PrepareData();
		var perfume = await GetFirst();
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/perfumes/{perfume.Id}");
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<PerfumeWithWornStatsDto>();
		Assert.NotNull(perfumes);
	}

	[Fact]
	public async Task GetPerfume_NotFound() {
		await PrepareData();
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/perfumes/{int.MaxValue}");
		Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	}

	[Fact]
	public async Task GetPerfumes() {
		await PrepareData();
		var client = factory.CreateClient();

		var response = await client.GetAsync("/api/perfumes");
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<PerfumeWithWornStatsDto>>();
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task UpdatePerfume() {
		await PrepareData();
		var client = factory.CreateClient();
		var perfume = await GetFirst();
		var dto = perfume.Adapt<PerfumeDto>();
		var content = JsonContent.Create(dto);
		var response = await client.PutAsync($"/api/perfumes/{dto.Id}", content);
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<PerfumeDto>();
		Assert.NotNull(perfumes);
	}

	[Fact]
	public async Task UpdatePerfumeGuid() {
		await PrepareData();
		var client = factory.CreateClient();
		var perfume = await GetFirst();
		var dto = new ImageGuidDto(perfume.Id, Guid.NewGuid().ToString());
		var content = JsonContent.Create(dto);
		var response = await client.PutAsync($"/api/perfumes/imageguid", content);
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<PerfumeDto>();
		Assert.NotNull(perfumes);
	}

	[Fact]
	public async Task DeletePerfume() {
		await PrepareData();
		var perfume = await GetFirst();
		var client = factory.CreateClient();
		var response = await client.DeleteAsync($"/api/perfumes/{perfume.Id}");
		response.EnsureSuccessStatusCode();
		Assert.True(true);
	}

	[Fact]
	public async Task AddPerfume() {
		await PrepareData();
		var client = factory.CreateClient();
		var dto = new PerfumeDto(0, "House3", "Perfume3", 5, "Notes", 50, "", true, true, false, false, new());
		var content = JsonContent.Create(dto);
		var response = await client.PostAsync($"/api/perfumes", content);
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<PerfumeDto>();
		Assert.NotNull(perfumes);
	}

	[Fact]
	public async Task GetStats() {
		await PrepareData();
		var client = factory.CreateClient();

		var response = await client.GetAsync("/api/perfumes/stats");
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<PerfumeStatDto>();
		Assert.NotNull(perfumes);
	}

	[Fact]
	public async Task GetFullText() {
		await PrepareData();
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/perfumes/fulltext/{perfumeSeed[0].PerfumeName}");
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<PerfumeWithWornStatsDto>>();
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}
}
