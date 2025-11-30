using PerfumeTracker.xTests.Fixture;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Health Tests")]
public class HealthCollection : ICollectionFixture<HealthFixture>;

public class HealthFixture : DbFixture {
	public HealthFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) { }
}

[Collection("Health Tests")]
public class HealthTests {
	private readonly HealthFixture _fixture;
	public HealthTests(HealthFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task GetHealth() {
		var client = _fixture.Factory.CreateClient();
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
