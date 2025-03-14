using Mapster;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Dto;
using PerfumeTracker.Server.Models;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;
using System.Net;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests;
public class PerfumePlaylistTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = factory.Services.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
				if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
				var sql = "truncate table \"public\".\"Perfume\" cascade; " +
					"truncate table \"public\".\"PerfumePerfumePlayList\" cascade; " +
					"truncate table \"public\".\"PerfumePlayList\" cascade";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Perfumes.AddRange(perfumeSeed);
				context.PerfumePlayLists.AddRange(perfumePlaylistSeed);
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

	static List<PerfumePlayList> perfumePlaylistSeed = new List<PerfumePlayList> {
			new PerfumePlayList { Name = "Default", Perfumes = new List<Perfume> { perfumeSeed[0], perfumeSeed[1] } },
			new PerfumePlayList { Name = "Work", Perfumes = new List<Perfume> { perfumeSeed[0] } }
		};


	//private async Task<Perfume> GetFirst() {
	//	using var scope = factory.Services.CreateScope();
	//	using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
	//	return await context.Perfumes.FirstAsync();
	//}

	//[Fact]
	//public async Task GetPerfume() {
	//	await PrepareData();
	//	var perfume = await GetFirst();
	//	var client = factory.CreateClient();
	//	var response = await client.GetAsync($"/api/perfumes/{perfume.Id}");
	//	response.EnsureSuccessStatusCode();

	//	var perfumes = await response.Content.ReadFromJsonAsync<PerfumeWithWornStatsDto>();
	//	Assert.NotNull(perfumes);
	//}

	//[Fact]
	//public async Task GetPerfume_NotFound() {
	//	await PrepareData();
	//	var client = factory.CreateClient();
	//	var response = await client.GetAsync($"/api/perfumes/{int.MaxValue}");
	//	Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
	//}

	//[Fact]
	//public async Task GetPerfumes() {
	//	await PrepareData();
	//	var client = factory.CreateClient();

	//	var response = await client.GetAsync("/api/perfumes");
	//	response.EnsureSuccessStatusCode();

	//	var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<PerfumeWithWornStatsDto>>();
	//	Assert.NotNull(perfumes);
	//	Assert.NotEmpty(perfumes);
	//}

	//[Fact]
	//public async Task UpdatePerfume() {
	//	await PrepareData();
	//	var client = factory.CreateClient();
	//	var perfume = await GetFirst();
	//	perfume.PerfumeTags.Add(new PerfumeTag() { Perfume = perfume, Tag = tagSeed[2] });
	//	var dto = perfume.Adapt<PerfumeDto>();
	//	var content = JsonContent.Create(dto);
	//	var response = await client.PutAsync($"/api/perfumes/{dto.Id}", content);
	//	response.EnsureSuccessStatusCode();

	//	var perfumes = await response.Content.ReadFromJsonAsync<PerfumeDto>();
	//	Assert.NotNull(perfumes);
	//}

	//[Fact]
	//public async Task UpdatePerfumeGuid() {
	//	await PrepareData();
	//	var client = factory.CreateClient();
	//	var perfume = await GetFirst();
	//	var dto = new ImageGuidDto(perfume.Id, Guid.NewGuid().ToString());
	//	var content = JsonContent.Create(dto);
	//	var response = await client.PutAsync($"/api/perfumes/imageguid", content);
	//	response.EnsureSuccessStatusCode();

	//	var perfumes = await response.Content.ReadFromJsonAsync<PerfumeDto>();
	//	Assert.NotNull(perfumes);
	//}

	//[Fact]
	//public async Task DeletePerfume() {
	//	await PrepareData();
	//	var perfume = await GetFirst();
	//	var client = factory.CreateClient();
	//	var response = await client.DeleteAsync($"/api/perfumes/{perfume.Id}");
	//	response.EnsureSuccessStatusCode();
	//	Assert.True(true);
	//}

	[Fact]
	public async Task AddPerfumePlaylist() {
		await PrepareData();
		var client = factory.CreateClient();
		var dto = new PerfumePlaylistDto("Third", new List<PerfumeDto> { perfumeSeed[0].Adapt<PerfumeDto>() }, DateTime.Now, null);
		var content = JsonContent.Create(dto);
		var response = await client.PostAsync($"/api/perfume-playlists", content);
		response.EnsureSuccessStatusCode();

		var perfumePlaylist = await response.Content.ReadFromJsonAsync<PerfumePlaylistDto>();
		Assert.NotNull(perfumePlaylist);
	}
}
