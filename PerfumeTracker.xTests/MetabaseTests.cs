using Microsoft.AspNetCore.Mvc.Testing;

namespace PerfumeTracker.xTests;
public class MetabaseTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>> {

	[Fact]
	public async Task GetMetabaseToken() {
		var client = factory.CreateClient();
		var response = await client.GetAsync($"/api/metabase");
		response.EnsureSuccessStatusCode();

		var token = await response.Content.ReadAsStringAsync();
		Assert.NotEmpty(token);
	}
}
