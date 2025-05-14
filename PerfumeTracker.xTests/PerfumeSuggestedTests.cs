using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests;

public class PerfumeSuggestedTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = factory.Services.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
				if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
				var sql = "truncate table \"public\".\"PerfumeEvent\" cascade; truncate table \"public\".\"Perfume\" cascade;  truncate table \"public\".\"PerfumeSuggested\" cascade;";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Perfumes.AddRange(perfumeSeed);
				context.PerfumeEvents.Add(new PerfumeWorn() {
					Perfume = perfumeSeed[0],
					CreatedAt = DateTime.UtcNow,
					EventDate = DateTime.UtcNow,
					Type = PerfumeWorn.PerfumeEventType.Worn
				});
				context.PerfumeSuggesteds.Add(new PerfumeSuggested() {
					Perfume = perfumeSeed[1],
					CreatedAt = DateTime.UtcNow.AddDays(-2)
				});
				await context.SaveChangesAsync();
				dbUp = true;
			}
		}
		finally {
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

	[Fact]
	public async Task GetPerfumeSuggestion() {
		await PrepareData();
		var client = factory.CreateClient();
		var queryParams = QueryString.Create(new Dictionary<string, string?>() {
			{ "dayFilter", "5" },
			{ "minimumRating", "8" }
		});
		var response = await client.GetAsync("/api/perfumesuggesteds" + queryParams);
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<int>();
		Assert.True(perfumes > 0);
	}

}
