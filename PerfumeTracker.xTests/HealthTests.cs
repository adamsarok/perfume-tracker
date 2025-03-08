using Mapster;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;
using System.Net;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests;
public class HealthTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {

	[Fact]
	public async Task GetHealth() {
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/health");
		response.EnsureSuccessStatusCode();

		var result = await response.Content.ReadFromJsonAsync<HealthCheckDTO>();
		Assert.NotNull(result);
		Assert.Equal("Healthy", result.status);
		Assert.Equal("Healthy", result.entries.npgsql.status);
	}

	public record Data();
	public record Entries(
		Npgsql npgsql
	);
	public record Npgsql(
		Data data,
		string description,
		string duration,
		string exception,
		string status,
		IReadOnlyList<object> tags
	);
	public record HealthCheckDTO(
		string status,
		string totalDuration,
		Entries entries
	);
}
