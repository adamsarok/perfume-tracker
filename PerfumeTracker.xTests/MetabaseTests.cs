using Mapster;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;
using System.Net;
using System.Net.Http.Json;

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
