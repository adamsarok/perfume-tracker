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

public class RecommendationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = factory.Services.CreateScope();
				using var context = scope.ServiceProvider.GetRequiredService<PerfumetrackerContext>();
				if (!context.Database.GetDbConnection().Database.ToLower().Contains("test")) throw new Exception("Live database connected!");
				var sql = "truncate table \"public\".\"Recommendation\" cascade";
				await context.Database.ExecuteSqlRawAsync(sql);
				context.Recommendations.AddRange(recommendationsSeed);
				await context.SaveChangesAsync();
				dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	static List<Recommendation> recommendationsSeed = new List<Recommendation>() {
		new Recommendation() { Query = "query1", Recommendations = "recommendations1", Created_At = DateTime.UtcNow },
	};


	[Fact]
	public async Task GetRecommendation() {
		await PrepareData();
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/recommendations/{recommendationsSeed[0].Id}");
		response.EnsureSuccessStatusCode();

		var recommendations = await response.Content.ReadFromJsonAsync<Recommendation>();
		Assert.NotNull(recommendations);
	}


	[Fact]
	public async Task GetRecommendations() {
		await PrepareData();
		var client = factory.CreateClient();
		var queryParams = QueryString.Create(new Dictionary<string, string?>() {
			{ "dayFilter", "5" }
		});
		var response = await client.GetAsync("/api/recommendations" + queryParams);
		response.EnsureSuccessStatusCode();

		var recommendations = await response.Content.ReadFromJsonAsync<IEnumerable<Recommendation>>();
		Assert.NotNull(recommendations);
		Assert.NotEmpty(recommendations);
	}

	[Fact]
	public async Task AddRecommendation() {
		await PrepareData();
		var client = factory.CreateClient();
		var dto = new RecommendationUploadDto("Query2", "Recommendation2");
		var content = JsonContent.Create(dto);
		var response = await client.PostAsync($"/api/recommendations", content);
		response.EnsureSuccessStatusCode();

		var recommendations = await response.Content.ReadFromJsonAsync<PerfumeDto>();
		Assert.NotNull(recommendations);
	}
}
