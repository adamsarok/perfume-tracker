using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests.Tests;

public class HealthTests : IClassFixture<WebApplicationFactory<Program>> {
	private readonly WebApplicationFactory<Program> _factory;

	public HealthTests(WebApplicationFactory<Program> factory) {
		_factory = factory;
	}

	[Fact]
	public async Task GetHealth() {
		var client = _factory.CreateClient();
		var response = await client.GetAsync($"/api/health");
		response.EnsureSuccessStatusCode();

		var result = await response.Content.ReadFromJsonAsync<HealthCheckDto>();
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
	public record HealthCheckDto(
		string status,
		string totalDuration,
		Entries entries
	);
}
