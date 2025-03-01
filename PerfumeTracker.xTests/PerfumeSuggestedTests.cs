using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Dto;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;
using System.Net;
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
				using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
				var sql = "truncate table \"public\".\"PerfumeWorn\" cascade; truncate table \"public\".\"Perfume\" cascade;  truncate table \"public\".\"PerfumeSuggested\" cascade;";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Perfumes.AddRange(perfumeSeed);
				context.PerfumeWorns.Add(new PerfumeWorn() {
					Perfume = perfumeSeed[0],
					Created_At = DateTime.UtcNow
				});
				context.PerfumeSuggesteds.Add(new PerfumeSuggested() {
					Perfume = perfumeSeed[1],
					Created_At = DateTime.UtcNow.AddDays(-2)
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
		var response = await client.GetAsync("/api/perfumesuggesteds/5");
		response.EnsureSuccessStatusCode();

		var perfumes = await response.Content.ReadFromJsonAsync<int>();
		Assert.True(perfumes > 0);
	}

}
