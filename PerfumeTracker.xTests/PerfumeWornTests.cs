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
				using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
				if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
				var sql = "truncate table \"public\".\"PerfumeEvent\" cascade; truncate table \"public\".\"Perfume\" cascade;";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Perfumes.AddRange(perfumeSeed);
				context.PerfumeEvents.Add(new PerfumeEvent() {
					Perfume = perfumeSeed[0],
					CreatedAt = DateTime.UtcNow,
					EventDate = DateTime.UtcNow,
					Type = PerfumeEvent.PerfumeEventType.Worn
				});
				context.PerfumeEvents.Add(new PerfumeEvent() {
					Perfume = perfumeSeed[1],
					CreatedAt = DateTime.UtcNow.AddDays(-1),
					EventDate = DateTime.UtcNow.AddDays(-1),
					Type = PerfumeEvent.PerfumeEventType.Worn
				});
				await context.SaveChangesAsync();
				dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	static List<Perfume> perfumeSeed = new List<Perfume> {
			new Perfume { Id = Guid.NewGuid(), House = "House1", PerfumeName = "Perfume1", Rating = 10
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("Perfume1")
			},
			new Perfume { Id = Guid.NewGuid(), House = "House2", PerfumeName = "Perfume2", Rating = 1
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("Perfume2")
			},
			new Perfume { Id = Guid.NewGuid(), House = "House2", PerfumeName = "NotWornPerfume", Rating = 1
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("NotWornPerfume")
			},
		};

	[Fact]
	public async Task GetPerfumeWorns() {
		await PrepareData();
		var client = factory.CreateClient();
		var queryParams = QueryString.Create(new Dictionary<string, string?>() {
			{ "cursor", "0" },
			{ "pageSize", "20" }
		});
		var response = await client.GetAsync("/api/perfume-events/worn-perfumes" + queryParams);
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<PerfumeEventDownloadDto>>();
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task GetPerfumesBefore() {
		await PrepareData();
		var client = factory.CreateClient();
		var response = await client.GetAsync("/api/perfume-events/worn-perfumes/5");
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<Guid>>();
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task DeletePerfumeWorn() {
		await PrepareData();
		using var scope = factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var worn = context.PerfumeEvents.First();
		var client = factory.CreateClient();
		var response = await client.DeleteAsync($"/api/perfume-events/{worn.Id}");
		response.EnsureSuccessStatusCode();
		var ev = await context.PerfumeEvents.FindAsync(worn.Id);
		await context.Entry(ev).ReloadAsync();
		Assert.True(ev.IsDeleted);
	}

	[Fact]
	public async Task AddPerfumeWorn() {
		await PrepareData();
		var client = factory.CreateClient();
		var dto = new PerfumeEventUploadDto(perfumeSeed[2].Id, DateTime.UtcNow, PerfumeEvent.PerfumeEventType.Worn, 0.05m, false);
		var content = JsonContent.Create(dto);
		var response = await client.PostAsync($"/api/perfume-events", content);
		response.EnsureSuccessStatusCode();
		var ev = await response.Content.ReadFromJsonAsync<PerfumeEventDownloadDto>();
		using var scope = factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		Assert.True(await context.PerfumeEvents.AnyAsync(x => x.Id == ev.Id));
	}
}
