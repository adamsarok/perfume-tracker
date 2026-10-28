using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;
using PerfumeTracker.Server.Dto;
using PerfumeTracker.Server.Features.ChatAgent.Services;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Tags;
using PerfumeTracker.Server.Models;
using PerfumeTracker.xTests.Fixture;
using static PerfumeTracker.Server.Features.Perfumes.GetPerfumeRecommendations;

namespace PerfumeTracker.xTests.Tests;

// Drives ChatAgentTools through ExecuteToolCall against real rows: filter clauses and orderings,
// ownership lookups, wardrobe gap buckets, marketplace offer filters, tool discovery and bad tool calls.
// The test tenant is shared with other fixtures, so every assertion is scoped to this fixture's
// own houses, brands, tags and note groups.
public class ChatAgentToolsFixture : DbFixture {
	private static readonly string Uid = Guid.NewGuid().ToString("N");
	private static readonly string Prefix = $"ChatTools {Uid}";

	public Guid IsolatedTenantId { get; } = Guid.NewGuid();

	public string FilterHouse { get; } = $"{Prefix} Filter";
	public string RatedHouse { get; } = $"{Prefix} Rated";
	public string House { get; } = $"{Prefix} House";
	public string PrefixedBrand { get; } = $"{Prefix} Brand";
	public string OwnedName { get; } = "Velvet Orris Reserve";
	public string PrefixedPerfumeName { get; } = "Amber Diary";
	public string CustomNoteGroup { get; } = $"{Prefix} Notes";
	public string TagPrefix { get; } = $"ChatToolsTag {Uid}";
	public string SellerA { get; } = $"{Prefix} Seller A";
	public string SellerB { get; } = $"{Prefix} Seller B";
	public string SourceA { get; } = $"{Prefix} Source A";
	public string SourceB { get; } = $"{Prefix} Source B";
	public string TextQueryNeedle { get; } = $"needle-{Uid}";
	public string WardrobePrefix { get; } = $"{Prefix} Wardrobe";

	public Guid OwnedPerfumeId { get; } = Guid.NewGuid();
	public Guid PrefixedPerfumeId { get; } = Guid.NewGuid();
	public Guid AlphaOfferId { get; } = Guid.NewGuid();
	public Guid BetaOfferId { get; } = Guid.NewGuid();
	public Guid GammaOfferId { get; } = Guid.NewGuid();
	public Guid DeltaOfferId { get; } = Guid.NewGuid();
	public Guid ZetaOfferId { get; } = Guid.NewGuid();
	public Guid PrefixedOfferId { get; } = Guid.NewGuid();

	public string[] FilterPerfumeNames { get; } = ["aaa", "bbb", "ccc", "ddd"];

	private PerfumeTrackerContext _db = null!;

	public override async Task SeedTestData(PerfumeTrackerContext context) {
		_db = context;
		// Fully isolate this fixture tenant: the tool queries are tenant-global (wardrobe stats,
		// marketplace availability), so deterministic expectations require owning the whole tenant.
		TenantProvider.MockTenantId = IsolatedTenantId;
		var now = DateTime.UtcNow;

		// Filter fixtures: distinct values for every orderable column, one house.
		var filterRatings = new[] { 1m, 3m, 5m, 4m };
		var filterWears = new[] { 10, 2, 7, 2 };
		var filterLastWorn = new DateTime?[] { now.AddDays(-200), null, now.AddDays(-20), now.AddDays(-90) };
		var filterMlLeft = new[] { 10m, 90m, 50m, 30m };
		for (var i = 0; i < FilterPerfumeNames.Length; i++) {
			AddPerfume(Guid.NewGuid(), FilterHouse, FilterPerfumeNames[i], filterRatings[i], filterWears[i],
				filterLastWorn[i], 100m, filterMlLeft[i]);
		}

		// Owned perfumes used by ownership checks, ratings and the marketplace owned exclusions.
		AddPerfume(OwnedPerfumeId, House, OwnedName, 4m, 3, now.AddDays(-5), 100m, 50m);
		AddPerfume(PrefixedPerfumeId, PrefixedBrand, PrefixedPerfumeName, 2m, 1, now.AddDays(-40), 100m, 40m);

		// Tags: real note groups for the gap buckets plus non-core, missing-note-group and deleted fixtures.
		var woodyTag = AddTag($"{TagPrefix} woody", "Woody");
		var citrusTag = AddTag($"{TagPrefix} citrus", "Citrus");
		var leatherTag = AddTag($"{TagPrefix} leather", "Leather");
		var customTag = AddTag($"{TagPrefix} custom", CustomNoteGroup);
		var otherTag = AddTag($"{TagPrefix} other", NoteGroups.Other);
		var noGroupTag = AddTag($"{TagPrefix} nogroup", null);
		var deletedTag = AddTag($"{TagPrefix} deleted", "Woody", isDeleted: true);

		// Wardrobe fixtures: Woody gets 6 (strong), Leather 3 (balanced), Citrus 1 (thin).
		// Wardrobe 1 also carries the custom note group so the tagsAll filter has a positive case.
		Guid? wardrobeFirstId = null;
		for (var i = 1; i <= 6; i++) {
			var wardrobe = AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} {i}", i, i, now.AddDays(-i), 100m, 50m);
			if (i == 1) wardrobeFirstId = wardrobe.Id;
			Link(woodyTag, wardrobe.Id);
		}
		if (wardrobeFirstId is Guid firstWardrobeId) Link(customTag, firstWardrobeId);
		for (var i = 1; i <= 3; i++) {
			Link(leatherTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} leather {i}", 5m, 1,
				now.AddDays(-30), 100m, 50m).Id);
		}
		Link(citrusTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} thin", 3m, 0, null, 100m, 50m).Id);
		Link(customTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} custom", 2m, 4, now.AddDays(-60), 100m, 50m).Id);
		// Note group "Other", a missing note group and a soft deleted link all count as ungrouped.
		Link(otherTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} other note", 6m, 1, now.AddDays(-3), 100m, 50m).Id);
		Link(noGroupTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} no group", 7m, 1, now.AddDays(-8), 100m, 50m).Id);
		Link(woodyTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} soft link", 8m, 1, now.AddDays(-9), 100m, 50m).Id,
			isDeleted: true);
		Link(deletedTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} deleted tag", 9m, 1, now.AddDays(-11), 100m, 50m).Id);
		AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} untagged", 10m, 1, now.AddDays(-12), 100m, 50m);
		// Empty bottle and soft deleted perfumes are ignored by the wardrobe analysis.
		Link(woodyTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} empty", 11m, 1, now.AddDays(-13), 100m, 0m).Id);
		Link(woodyTag, AddPerfume(Guid.NewGuid(), House, $"{WardrobePrefix} deleted", 12m, 1, now.AddDays(-14), 100m, 50m,
			isDeleted: true).Id);

		// Ratings: newest non-blank comment wins, blanks and other perfumes are ignored.
		var otherRated = AddPerfume(Guid.NewGuid(), RatedHouse, "Rated Perfume", 3m, 1, null, 100m, 25m);
		_db.PerfumeRatings.AddRange([
			AddRating(OwnedPerfumeId, 4m, "great bottle", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
			AddRating(OwnedPerfumeId, 5m, "  \n  ", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
			AddRating(otherRated.Id, 1m, "unrelated note", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc))
		]);

		// Marketplace offers covering every filter, the status default, owned exclusions and clamping.
		AddOffer(AlphaOfferId, SourceA, SellerA, $"{Prefix} Brand Alpha", "Alpha Amber Diary", "Active",
			new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc));
		AddOffer(BetaOfferId, SourceA, SellerA, $"{Prefix} Brand Beta", "Beta Bergamot Diary", "Sold",
			new DateTime(2024, 7, 1, 0, 0, 0, DateTimeKind.Utc));
		AddOffer(GammaOfferId, SourceB, SellerB, $"{Prefix} Brand Gamma", "Gamma Garden Diary", "Active",
			null, description: $"{TextQueryNeedle} gamma description");
		AddOffer(DeltaOfferId, SourceB, SellerB, House, OwnedName, "Active",
			new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Utc));
		AddOffer(ZetaOfferId, SourceB, SellerB, House, "Zeta Zest Diary", "Active",
			new DateTime(2024, 4, 1, 0, 0, 0, DateTimeKind.Utc), matchedPerfumeId: OwnedPerfumeId);
		AddOffer(PrefixedOfferId, $"{Prefix} Source C", $"{Prefix} Seller C", PrefixedBrand,
			$"{PrefixedBrand} {PrefixedPerfumeName}", "Archived",
			new DateTime(2024, 3, 1, 0, 0, 0, DateTimeKind.Utc));

		await context.SaveChangesAsync();
	}

	private Perfume AddPerfume(Guid id, string house, string name, decimal rating, int wearCount,
		DateTime? lastWorn, decimal ml, decimal mlLeft, bool isDeleted = false) {
		var perfume = new Perfume {
			Id = id,
			UserId = TenantProvider.MockTenantId!.Value,
			House = house,
			PerfumeName = name,
			Family = "Family",
			Parfumeur = "Parfumeur",
			Ml = ml,
			MlLeft = mlLeft,
			LatestRating = rating,
			WearCount = wearCount,
			LastWorn = lastWorn,
			IsDeleted = isDeleted
		};
		_db.Perfumes.Add(perfume);
		return perfume;
	}

	private Tag AddTag(string tagName, string? noteGroup, bool isDeleted = false) {
		var tag = new Tag {
			Id = Guid.NewGuid(),
			UserId = TenantProvider.MockTenantId!.Value,
			TagName = tagName,
			NoteGroup = noteGroup,
			IsDeleted = isDeleted
		};
		_db.Tags.Add(tag);
		return tag;
	}

	private void Link(Tag tag, Guid perfumeId, bool isDeleted = false) {
		_db.PerfumeTags.Add(new PerfumeTag {
			Id = Guid.NewGuid(),
			UserId = TenantProvider.MockTenantId!.Value,
			TagId = tag.Id,
			PerfumeId = perfumeId,
			IsDeleted = isDeleted
		});
	}

	private PerfumeRating AddRating(Guid perfumeId, decimal rating, string comment, DateTime ratingDate) => new() {
		Id = Guid.NewGuid(),
		UserId = TenantProvider.MockTenantId!.Value,
		PerfumeId = perfumeId,
		Rating = rating,
		Comment = comment,
		RatingDate = ratingDate
	};

	private void AddOffer(Guid id, string source, string sellerName, string brandName, string productName,
		string status, DateTime? listedAt, Guid? matchedPerfumeId = null, string? description = null) {
		_db.MarketplaceOffers.Add(new MarketplaceOffer {
			Id = id,
			UserId = TenantProvider.MockTenantId!.Value,
			Source = source,
			SourceOfferId = $"offer-{id:N}",
			SourceUrl = $"https://example.test/{id:N}",
			SellerName = sellerName,
			BrandName = brandName,
			ProductName = productName,
			OfferTitle = $"{productName} offer",
			PriceText = "$10",
			SizeText = "10 ml",
			Description = description,
			Status = status,
			ListedAt = listedAt,
			// MatchConfidence is set so the matcher background service skips these seeded rows.
			MatchConfidence = 0m,
			MatchedPerfumeId = matchedPerfumeId
		});
	}
}

public class ChatAgentToolsTests(ChatAgentToolsFixture fixture) : IClassFixture<ChatAgentToolsFixture> {
	private const int MaxResults = 50;

	// Fresh scope and memory cache per call: the tool list and marketplace availability are cached per tenant.
	private async Task<string> RunToolRaw(string toolName, string argumentsJson, Mock<IPerfumeRecommender>? recommender = null) {
		using var scope = fixture.Factory.Services.CreateScope();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var tools = new ChatAgentTools(scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>(),
			recommender?.Object ?? Mock.Of<IPerfumeRecommender>(), Options.Create(new ChatAgentOptions { MaxResultsPerToolCall = MaxResults }), cache);
		var call = ChatToolCall.CreateFunctionToolCall("test", toolName, BinaryData.FromString(argumentsJson));
		return await tools.ExecuteToolCall(call, TestContext.Current.CancellationToken);
	}

	private Task<string> RunTool(string toolName, object arguments, Mock<IPerfumeRecommender>? recommender = null) =>
		RunToolRaw(toolName, JsonSerializer.Serialize(arguments), recommender);

	private async Task<List<PerfumeLlmDto>> Filter(object arguments) =>
		JsonSerializer.Deserialize<List<PerfumeLlmDto>>(await RunTool("filter_owned_perfumes", arguments))!;

	private async Task<List<string>> OrderedNames(object arguments) =>
		(await Filter(arguments)).Select(p => p.PerfumeName).ToList();

	private async Task<List<MarketplaceOfferLlmDto>> Offers(object arguments) =>
		JsonSerializer.Deserialize<List<MarketplaceOfferLlmDto>>(await RunTool("search_marketplace_offers", arguments))!;

	private async Task<T> WithForeignTenantAsync<T>(Func<Task<T>> action) {
		var original = fixture.TenantProvider.MockTenantId;
		try {
			fixture.TenantProvider.MockTenantId = Guid.NewGuid();
			return await action();
		} finally {
			fixture.TenantProvider.MockTenantId = original;
		}
	}

	[Theory]
	[InlineData("rating", "desc", "ccc,ddd,bbb,aaa")]
	[InlineData("rating", "asc", "aaa,bbb,ddd,ccc")]
	[InlineData("wearCount", "desc", "aaa,ccc,ddd,bbb")]
	[InlineData("wearCount", "asc", "ddd,bbb,ccc,aaa")]
	[InlineData("lastWorn", "desc", "bbb,ccc,ddd,aaa")]
	[InlineData("lastWorn", "asc", "bbb,aaa,ddd,ccc")]
	[InlineData("mlLeft", "desc", "bbb,ccc,ddd,aaa")]
	[InlineData("mlLeft", "asc", "aaa,ddd,ccc,bbb")]
	[InlineData("house", "desc", "aaa,bbb,ccc,ddd")]
	[InlineData("house", "asc", "aaa,bbb,ccc,ddd")]
	[InlineData("perfumeName", "desc", "ddd,ccc,bbb,aaa")]
	[InlineData("perfumeName", "asc", "aaa,bbb,ccc,ddd")]
	[InlineData("not-a-field", "desc", "ccc,ddd,bbb,aaa")]
	public async Task Ordering_SupportsEveryFieldAndDirection(string orderBy, string orderDirection, string expected) {
		var names = await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, orderBy, orderDirection });
		Assert.Equal(expected.Split(','), names);
	}

	[Fact]
	public async Task Ordering_DefaultsToRatingDescending() {
		var names = await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults });
		Assert.Equal(new[] { "ccc", "ddd", "bbb", "aaa" }, names);
	}

	[Fact]
	public async Task Count_IsClampedToTheConfiguredMaximum() {
		Assert.Single(await Filter(new { house = fixture.FilterHouse, count = 0 }));
		var all = await Filter(new { count = 999 });
		Assert.Equal(23, all.Count);
		Assert.DoesNotContain(all, p => p.PerfumeName == $"{fixture.WardrobePrefix} empty");
		Assert.DoesNotContain(all, p => p.PerfumeName == $"{fixture.WardrobePrefix} deleted");
	}

	[Fact]
	public async Task Ungrouped_And_EmptyBottle_Fixtures_BelongToTheTenant() {
		var untagged = await Filter(new { count = MaxResults });
		Assert.Equal($"{fixture.WardrobePrefix} untagged", untagged[0].PerfumeName);
		Assert.Equal(23, untagged.Count);
	}

	[Fact]
	public async Task Filter_RespectsCurrentTenant() {
		var results = await WithForeignTenantAsync(() => Filter(new { house = fixture.FilterHouse, count = MaxResults }));
		Assert.Empty(results);
	}

	[Fact]
	public async Task House_And_Family_Filters_AreCaseInsensitiveEquality() {
		Assert.Equal(4, (await Filter(new { house = fixture.FilterHouse.ToUpperInvariant(), count = MaxResults })).Count);
		Assert.Equal(4, (await Filter(new { family = "family", house = fixture.FilterHouse, count = MaxResults })).Count);
		Assert.Empty(await Filter(new { house = $"{fixture.FilterHouse}x", count = MaxResults }));
		Assert.Empty(await Filter(new { family = "missing", count = MaxResults }));
	}

	[Fact]
	public async Task Rating_WearAndAvailability_Predicates_FilterRows() {
		Assert.Equal(new[] { "ccc", "ddd", "bbb" }, await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, minRating = 3 }));
		Assert.Equal(new[] { "bbb", "aaa" }, await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, maxRating = 3 }));
		Assert.Equal(new[] { "ccc" }, await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, ratingAbove = 4 }));
		Assert.Equal(new[] { "ccc", "aaa" }, await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, minWearCount = 5 }));
		Assert.Equal(new[] { "ddd", "bbb" }, await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, maxWearCount = 2 }));
	}

	[Fact]
	public async Task WornWindows_UseUtcCutoffsAndKeepUnknownDates() {
		Assert.Equal(new[] { "bbb", "aaa" }, await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, notWornInDays = 100 }));
		Assert.Equal(new[] { "ccc" }, await OrderedNames(new { house = fixture.FilterHouse, count = MaxResults, wornWithinDays = 30 }));
		var neverWorn = await Filter(new { count = MaxResults, neverWorn = true });
		Assert.Equal($"{fixture.WardrobePrefix} thin", Assert.Single(neverWorn).PerfumeName);
	}

	[Fact]
	public async Task TagFilters_MatchTagNamesAndRespectSoftDeletedLinks() {
		var woody = fixture.TagPrefix + " woody";
		var tagged = await Filter(new { count = MaxResults, tagsAny = new[] { woody.ToUpperInvariant() } });
		Assert.Equal(6, tagged.Count);
		Assert.All(tagged, p => Assert.StartsWith(fixture.WardrobePrefix, p.PerfumeName));
		Assert.DoesNotContain(tagged, p => p.PerfumeName == $"{fixture.WardrobePrefix} soft link");
		Assert.DoesNotContain(tagged, p => p.PerfumeName == $"{fixture.WardrobePrefix} empty");

		var includingEmpty = await Filter(new { count = MaxResults, tagsAny = new[] { woody }, onlyAvailable = false });
		Assert.Contains($"{fixture.WardrobePrefix} empty", includingEmpty.Select(p => p.PerfumeName));

		var both = await Filter(new { count = MaxResults, tagsAll = new[] { woody, $"{fixture.TagPrefix} custom" } });
		Assert.Equal($"{fixture.WardrobePrefix} 1", Assert.Single(both).PerfumeName);
		Assert.Empty(await Filter(new { count = MaxResults, tagsAll = new[] { woody, $"{fixture.TagPrefix} citrus" } }));
	}

	[Fact]
	public async Task MalformedArgumentTypes_AreIgnoredAndFallBackToDefaults() {
		var results = await Filter(new { count = "50", orderBy = 12, minRating = "3", neverWorn = "true", tagsAny = $"{fixture.TagPrefix} woody" });
		Assert.Equal(10, results.Count);
		Assert.Equal($"{fixture.WardrobePrefix} untagged", results[0].PerfumeName);
	}

	[Fact]
	public async Task InvalidPercentageThreshold_IsRejected() {
		await Assert.ThrowsAsync<ArgumentException>(() => Filter(new { remainingPercentBelow = 101 }));
	}

	private static PerfumeWithWornStatsDto RecommendationSource(string name, decimal ml, decimal mlLeft, string? lastComment, params string[] tags) =>
		new(new PerfumeDto(Guid.NewGuid(), "Recommendation House", name, "Family", "Parfumeur", ml, mlLeft, null,
				"https://example.test/image", tags.Select(t => new TagDto(t, null, Guid.NewGuid(), false, null)).ToList(),
				false, [], 3, 4m, null),
			1.5m, 2m, lastComment!);

	[Fact]
	public async Task Characteristics_ClampsCountAndForwardsPrompt() {
		var capturedCount = 0;
		var capturedPrompt = string.Empty;
		var recommender = new Mock<IPerfumeRecommender>();
		_ = recommender.Setup(r => r.GetRecommendationsForOccasionMoodPrompt(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.Callback<int, string, CancellationToken>((count, prompt, _) => {
				capturedCount = count;
				capturedPrompt = prompt;
			})
			.ReturnsAsync(Array.Empty<PerfumeRecommendationDto>());

		Assert.Empty(JsonSerializer.Deserialize<List<PerfumeLlmDto>>(
			await RunTool("search_owned_perfumes_by_characteristics", new { prompt = "cozy vanilla", count = 999 }, recommender))!);
		Assert.Equal(MaxResults, capturedCount);
		Assert.Equal("cozy vanilla", capturedPrompt);

		_ = await RunTool("search_owned_perfumes_by_characteristics", new { prompt = "woody" }, recommender);
		Assert.Equal(5, capturedCount);
	}

	[Fact]
	public async Task Characteristics_MapsRecommendationsToLlmDtos() {
		var recommendations = new[] {
			new PerfumeRecommendationDto(Guid.NewGuid(),
				RecommendationSource("Sorted Tags", 100m, 25m, "  ", "beta tag", "alpha tag"), RecommendationStrategy.MoodOrOccasion),
			new PerfumeRecommendationDto(Guid.NewGuid(),
				RecommendationSource("Unknown Capacity", 0m, 0m, "keeps comment"), RecommendationStrategy.Seasonal)
		};
		var recommender = new Mock<IPerfumeRecommender>();
		_ = recommender.Setup(r => r.GetRecommendationsForOccasionMoodPrompt(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(recommendations.AsEnumerable());

		var results = JsonSerializer.Deserialize<List<PerfumeLlmDto>>(
			await RunTool("search_owned_perfumes_by_characteristics", new { prompt = "cozy", count = 2 }, recommender))!;

		var sorted = results.Single(p => p.PerfumeName == "Sorted Tags");
		Assert.Null(sorted.LastComment);
		Assert.Equal(new[] { "alpha tag", "beta tag" }, sorted.Tags);
		Assert.Equal(25m, sorted.RemainingPercent);
		Assert.Equal(100m, sorted.Ml);

		var unknown = results.Single(p => p.PerfumeName == "Unknown Capacity");
		Assert.Equal("keeps comment", unknown.LastComment);
		Assert.Null(unknown.RemainingPercent);
	}

	[Fact]
	public async Task Characteristics_MissingPrompt_IsRejected() {
		await Assert.ThrowsAsync<ArgumentException>(() => RunTool("search_owned_perfumes_by_characteristics", new { prompt = (object?) null }));
		await Assert.ThrowsAsync<KeyNotFoundException>(() => RunTool("search_owned_perfumes_by_characteristics", new { }));
	}

	private async Task<List<PerfumeOwnershipCheckResult>> Ownership(object arguments) =>
		JsonSerializer.Deserialize<List<PerfumeOwnershipCheckResult>>(await RunTool("check_perfume_ownership", arguments))!;

	[Fact]
	public async Task Ownership_ReportsExactCaseInsensitiveMatches() {
		var results = await Ownership(new {
			perfumes = new[] {
				new { house = fixture.House, name = fixture.OwnedName },
				new { house = fixture.House.ToUpperInvariant(), name = fixture.OwnedName.ToUpperInvariant() }
			}
		});
		Assert.Equal(2, results.Count);
		Assert.All(results, r => Assert.True(r.IsOwned));
		Assert.Equal(fixture.House.ToUpperInvariant(), results[1].House);
	}

	[Fact]
	public async Task Ownership_ReportsFuzzyMatchesAboveBothThresholds() {
		var results = await Ownership(new {
			perfumes = new[] {
				new { house = $"{fixture.PrefixedBrand}s", name = "Amber Dairy" },
				new { house = "Some Unrelated House", name = "Zebra Zest Extreme" }
			}
		});
		Assert.True(results[0].IsOwned);
		Assert.False(results[1].IsOwned);
	}

	[Fact]
	public async Task Ownership_MissingPerfumes_IsRejected() {
		await Assert.ThrowsAsync<ArgumentException>(() => Ownership(new { perfumes = (object?) null }));
		await Assert.ThrowsAsync<KeyNotFoundException>(() => Ownership(new { }));
	}

	private Task<WardrobeGapAnalysis> Gaps() =>
		Task.FromResult(JsonSerializer.Deserialize<WardrobeGapAnalysis>(RunTool("analyze_wardrobe_gaps", new { }).GetAwaiter().GetResult())!);

	[Fact]
	public async Task WardrobeGaps_BucketsNoteGroupsByCoverage() {
		var gaps = await Gaps();

		Assert.Equal(23, gaps.OwnedPerfumeCount);
		Assert.Equal(12, gaps.UngroupedPerfumeCount);
		Assert.Equal(NoteGroups.WardrobeCoverage.Count - 3, gaps.MissingNoteGroups.Count);
		Assert.All(gaps.MissingNoteGroups, g => Assert.Equal(0, g.OwnedPerfumeCount));

		var thin = Assert.Single(gaps.ThinNoteGroups);
		Assert.Equal("Citrus", thin.NoteGroup);
		Assert.Equal(1, thin.OwnedPerfumeCount);
		Assert.Equal($"{fixture.WardrobePrefix} thin", Assert.Single(thin.ExamplePerfumes).PerfumeName);

		var balanced = Assert.Single(gaps.BalancedNoteGroups);
		Assert.Equal("Leather", balanced.NoteGroup);
		Assert.Equal(3, balanced.OwnedPerfumeCount);
		Assert.Equal(3, balanced.ExamplePerfumes.Count);

		var strong = Assert.Single(gaps.StrongNoteGroups);
		Assert.Equal("Woody", strong.NoteGroup);
		Assert.Equal(6, strong.OwnedPerfumeCount);
		Assert.Equal(new[] { 6, 5, 4, 3, 2 }, strong.ExamplePerfumes.Select(p => (int) p.Rating).ToArray());

		var nonCore = gaps.NonCoreNoteGroups.Single(g => g.NoteGroup == fixture.CustomNoteGroup);
		Assert.Equal(2, nonCore.OwnedPerfumeCount);
		Assert.Equal(new[] { $"{fixture.WardrobePrefix} custom", $"{fixture.WardrobePrefix} 1" },
			nonCore.ExamplePerfumes.Select(p => p.PerfumeName).ToArray());
	}

	[Fact]
	public async Task WardrobeGaps_SeesOnlyCurrentTenant() {
		var gaps = await WithForeignTenantAsync(Gaps);
		Assert.Equal(0, gaps.OwnedPerfumeCount);
		Assert.Equal(0, gaps.UngroupedPerfumeCount);
		Assert.Equal(NoteGroups.WardrobeCoverage.Count, gaps.MissingNoteGroups.Count);
		Assert.Empty(gaps.StrongNoteGroups);
		Assert.Empty(gaps.NonCoreNoteGroups);
	}

	[Fact]
	public async Task MarketplaceOffers_DefaultToActiveNonOwnedNewestFirst() {
		var results = await Offers(new { });
		Assert.Equal(new[] { "Gamma Garden Diary", "Alpha Amber Diary" }, results.Select(o => o.ProductName).ToArray());
		Assert.Equal($"https://example.test/{fixture.GammaOfferId:N}", results[0].SourceUrl);
	}

	[Fact]
	public async Task MarketplaceOffers_FiltersStatusSourceSellerBrandAndProduct() {
		Assert.Equal(new[] { "Beta Bergamot Diary" }, (await Offers(new { status = "sold" })).Select(o => o.ProductName).ToArray());
		Assert.Equal(new[] { "Alpha Amber Diary" }, (await Offers(new { source = fixture.SourceA })).Select(o => o.ProductName).ToArray());
		Assert.Equal(new[] { "Alpha Amber Diary" }, (await Offers(new { sellerName = "seller a" })).Select(o => o.ProductName).ToArray());
		Assert.Equal(new[] { "Alpha Amber Diary" }, (await Offers(new { brandName = "brand alpha" })).Select(o => o.ProductName).ToArray());
		Assert.Equal(new[] { "Beta Bergamot Diary" }, (await Offers(new { productName = "bergamot", status = "Sold" })).Select(o => o.ProductName).ToArray());
		Assert.Empty(await Offers(new { source = "no such source" }));
	}

	[Fact]
	public async Task MarketplaceOffers_TextQueryAndCountLimit() {
		Assert.Equal(new[] { "Gamma Garden Diary" }, (await Offers(new { query = fixture.TextQueryNeedle })).Select(o => o.ProductName).ToArray());
		Assert.Equal(new[] { "Gamma Garden Diary" }, (await Offers(new { count = 1 })).Select(o => o.ProductName).ToArray());
		Assert.Equal(new[] { "Gamma Garden Diary", "Alpha Amber Diary", "Velvet Orris Reserve", "Zeta Zest Diary" },
			(await Offers(new { excludeOwned = false })).Select(o => o.ProductName).ToArray());
	}

	[Fact]
	public async Task MarketplaceOffers_ExcludesOwnedByMatchedIdExactNameAndBrandPrefix() {
		var visible = (await Offers(new { })).Select(o => o.ProductName).ToList();
		Assert.DoesNotContain("Velvet Orris Reserve", visible);
		Assert.DoesNotContain("Zeta Zest Diary", visible);
		Assert.Empty(await Offers(new { status = "Archived" }));

		var archived = await Offers(new { status = "Archived", excludeOwned = false });
		Assert.Equal($"{fixture.PrefixedBrand} {fixture.PrefixedPerfumeName}", Assert.Single(archived).ProductName);
	}

	[Fact]
	public async Task MarketplaceOffers_SeesOnlyCurrentTenant() {
		Assert.Empty(await WithForeignTenantAsync(() => Offers(new { excludeOwned = false })));
	}

	private Task<(IReadOnlyList<string> ToolNames, bool HasOffers)> InspectTools() {
		using var scope = fixture.Factory.Services.CreateScope();
		using var cache = new MemoryCache(new MemoryCacheOptions());
		var tools = new ChatAgentTools(scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>(),
			Mock.Of<IPerfumeRecommender>(), Options.Create(new ChatAgentOptions { MaxResultsPerToolCall = MaxResults }), cache);
		return Task.FromResult<(IReadOnlyList<string> ToolNames, bool HasOffers)>(
			(tools.Tools.Select(t => t.FunctionName).ToList(), tools.HasMarketplaceOffers));
	}

	[Fact]
	public async Task ToolDiscovery_AddsTheMarketplaceToolOnlyWhenOffersExist() {
		var local = await InspectTools();
		Assert.True(local.HasOffers);
		Assert.Equal(5, local.ToolNames.Count);
		Assert.Contains("search_marketplace_offers", local.ToolNames);

		var foreign = await WithForeignTenantAsync(InspectTools);
		Assert.False(foreign.HasOffers);
		Assert.Equal(4, foreign.ToolNames.Count);
		Assert.DoesNotContain("search_marketplace_offers", foreign.ToolNames);
	}

	[Fact]
	public async Task UnknownTool_And_UnparsableArguments_AreRejected() {
		var error = await Assert.ThrowsAsync<InvalidOperationException>(() => RunTool("no_such_tool", new { }));
		Assert.Equal("Unknown tool: no_such_tool", error.Message);
		await Assert.ThrowsAsync<InvalidOperationException>(() => RunToolRaw("filter_owned_perfumes", "null"));
	}
}
