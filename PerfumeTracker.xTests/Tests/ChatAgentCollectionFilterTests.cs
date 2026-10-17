using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;
using PerfumeTracker.Server.Features.ChatAgent.Services;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

public class ChatAgentCollectionFilterFixture : DbFixture {
	public string House { get; } = $"Filter tests {Guid.NewGuid()}";

	public override async Task SeedTestData(PerfumeTrackerContext context) {
		var values = new (string Name, decimal Ml, decimal Left, decimal Rating)[] {
			("low", 100, 9, 4.3m), ("boundary", 50, 5, 4.2m),
			("sample", 2, 1.9m, 4.1m), ("average", 100, 2, 3),
			("unrated", 100, 1, 0), ("empty", 100, 0, 3.5m),
			("unknown", 0, 1, 4), ("negative", -1, 0, 4),
			("fraction", 3, 0.299m, 3.8m), ("full", 100, 100, 5)
		};
		foreach (var value in values) {
			var perfume = GeneratePerfumes(1)[0];
			perfume.House = House;
			perfume.PerfumeName = value.Name;
			perfume.Ml = value.Ml;
			perfume.MlLeft = value.Left;
			perfume.LatestRating = value.Rating;
			context.Perfumes.Add(perfume);
		}
		await context.SaveChangesAsync();
		await context.Perfumes.Where(p => p.House == House && p.PerfumeName == "unknown")
			.ExecuteUpdateAsync(update => update.SetProperty(p => p.Ml, 0m));
	}
}

public class ChatAgentCollectionFilterTests(ChatAgentCollectionFilterFixture fixture)
	: IClassFixture<ChatAgentCollectionFilterFixture> {
	private async Task<List<PerfumeLlmDto>> Filter(object arguments) {
		using var scope = fixture.Factory.Services.CreateScope();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var tools = new ChatAgentTools(scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>(),
			Mock.Of<IPerfumeRecommender>(), Options.Create(new ChatAgentOptions { MaxResultsPerToolCall = 50 }), cache);
		var call = ChatToolCall.CreateFunctionToolCall("test", "filter_owned_perfumes", BinaryData.FromString(JsonSerializer.Serialize(arguments)));
		return JsonSerializer.Deserialize<List<PerfumeLlmDto>>(await tools.ExecuteToolCall(call, TestContext.Current.CancellationToken))!;
	}

	[Fact]
	public async Task Rebuy_UsesStrictPercentageAndRating_IncludingEmptyBottles() {
		var results = await Filter(new { house = fixture.House, remainingPercentBelow = 10, ratingAbove = 3, onlyAvailable = false, count = 50 });
		Assert.Equal(new[] { "empty", "fraction", "low" }, results.Select(p => p.PerfumeName).Order().ToArray());
		Assert.Equal(9m, results.Single(p => p.PerfumeName == "low").RemainingPercent);
		Assert.Equal(0m, results.Single(p => p.PerfumeName == "empty").RemainingPercent);
		Assert.Equal(3m, results.Single(p => p.PerfumeName == "fraction").Ml);
	}

	[Fact]
	public async Task Rebuy_FiltersBeforeLimitingAndOrdering() {
		var results = await Filter(new { house = fixture.House, remainingPercentBelow = 10, ratingAbove = 3, onlyAvailable = false, count = 1, orderBy = "rating" });
		Assert.Equal("low", Assert.Single(results).PerfumeName);
	}

	[Fact]
	public async Task AvailabilityDefaultsToNonempty_AndRatingFilterIsOptional() {
		var results = await Filter(new { house = fixture.House, remainingPercentBelow = 10, count = 50 });
		Assert.Equal(new[] { "average", "fraction", "low", "unrated" }, results.Select(p => p.PerfumeName).Order().ToArray());
	}

	[Fact]
	public async Task Result_ReportsCapacityAndNullForInvalidPercentage() {
		var results = await Filter(new { house = fixture.House, onlyAvailable = false, count = 50 });
		Assert.Equal(95m, results.Single(p => p.PerfumeName == "sample").RemainingPercent);
		Assert.Null(results.Single(p => p.PerfumeName == "unknown").RemainingPercent);
		Assert.Null(results.Single(p => p.PerfumeName == "negative").RemainingPercent);
	}

	[Fact]
	public async Task Filter_RespectsCurrentTenant() {
		var original = fixture.TenantProvider.MockTenantId;
		try {
			fixture.TenantProvider.MockTenantId = Guid.NewGuid();
			Assert.Empty(await Filter(new { house = fixture.House, remainingPercentBelow = 10, onlyAvailable = false }));
		} finally {
			fixture.TenantProvider.MockTenantId = original;
		}
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(101)]
	public async Task InvalidPercentage_IsRejected(int threshold) {
		await Assert.ThrowsAsync<ArgumentException>(() => Filter(new { remainingPercentBelow = threshold }));
	}
}
