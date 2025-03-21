using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests;

public class PerfumeWornTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = factory.Services.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
				if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
				var sql = "truncate table \"public\".\"PerfumeWorn\" cascade; truncate table \"public\".\"Perfume\" cascade;";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Perfumes.AddRange(perfumeSeed);
				context.PerfumeWorns.Add(new PerfumeWorn() {
					Perfume = perfumeSeed[0],
					Created_At = DateTime.UtcNow
				});
				context.PerfumeWorns.Add(new PerfumeWorn() {
					Perfume = perfumeSeed[1],
					Created_At = DateTime.UtcNow.AddDays(-1)
				});
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
			new Perfume { Id = 0, House = "House2", PerfumeName = "NotWornPerfume", Rating = 1
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("NotWornPerfume")
			},
		};

	private async Task<PerfumeWorn> GetFirst() {
		using var scope = factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
		return await context.PerfumeWorns.FirstAsync();
	}

	[Fact]
	public async Task GetPerfumeWorns() {
		await PrepareData();
		var client = factory.CreateClient();
		var queryParams = QueryString.Create(new Dictionary<string, string?>() {
			{ "cursor", "0" },
			{ "pageSize", "20" }
		});
		var response = await client.GetAsync("/api/perfumeworns" + queryParams);
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<PerfumeWornDownloadDto>>();
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task GetPerfumesBefore() {
		await PrepareData();
		var client = factory.CreateClient();
		var response = await client.GetAsync("/api/perfumeworns/perfumesbefore/5");
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<int>>();
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task DeletePerfumeWorn() {
		await PrepareData();
		var worn = await GetFirst();
		var client = factory.CreateClient();
		var response = await client.DeleteAsync($"/api/perfumeworns/{worn.Id}");
		response.EnsureSuccessStatusCode();
		Assert.True(true);
	}

	[Fact]
	public async Task AddPerfumeWorn() {
		await PrepareData();
		var client = factory.CreateClient();
		var dto = new PerfumeWornUploadDto(perfumeSeed[2].Id, DateTime.UtcNow);
		var content = JsonContent.Create(dto);
		var response = await client.PostAsync($"/api/perfumeworns", content);
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<PerfumeWornDownloadDto>();
		Assert.NotNull(perfumes);
	}
}
