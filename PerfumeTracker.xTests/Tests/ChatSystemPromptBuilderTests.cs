using Microsoft.Extensions.Options;
using Moq;
using PerfumeTracker.Server.Features.ChatAgent.Services;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Features.Users.Services;

namespace PerfumeTracker.xTests.Tests;

public class ChatSystemPromptBuilderTests {
	private static ChatSystemPromptBuilder CreateBuilder(bool hasMarketplaceOffers, int maxResultsPerToolCall) {
		var stats = new UserStatsResponse(
			null, null, 0, 0, 0m, 0m, 0m, 0m,
			[new FavoritePerfumeDto(Guid.NewGuid(), "Cryinna", "Garden Lovers", 4.2m, 3)],
			[],
			[new FavoriteTagDto(Guid.NewGuid(), "Vanilla", "#ffffff", 5, 10m)],
			null, null, [], []);

		var statsService = new Mock<IUserStatsService>();
		_ = statsService.Setup(s => s.GetUserStats(It.IsAny<CancellationToken>())).ReturnsAsync(stats);

		var tools = new Mock<IChatAgentTools>();
		_ = tools.SetupGet(t => t.HasMarketplaceOffers).Returns(hasMarketplaceOffers);

		return new ChatSystemPromptBuilder(statsService.Object, tools.Object,
			Options.Create(new ChatAgentOptions { MaxResultsPerToolCall = maxResultsPerToolCall }));
	}

	[Fact]
	public async Task BuildAsync_IncludesFavoritesAndMarketplaceTooling() {
		var prompt = await CreateBuilder(hasMarketplaceOffers: true, maxResultsPerToolCall: 7).BuildAsync(TestContext.Current.CancellationToken);

		Assert.Contains("Vanilla", prompt);
		Assert.Contains($"Cryinna - Garden Lovers rated {4.2m:F1})", prompt);
		Assert.Contains("- search_marketplace_offers: Search local marketplace offers.", prompt);
		Assert.Contains("- Use search_marketplace_offers for marketplace offers", prompt);
	}

	[Fact]
	public async Task BuildAsync_OmitsMarketplaceLinesWhenNoOffers() {
		var prompt = await CreateBuilder(hasMarketplaceOffers: false, maxResultsPerToolCall: 7).BuildAsync(TestContext.Current.CancellationToken);

		Assert.DoesNotContain("search_marketplace_offers", prompt);
		Assert.Contains("- Use check_perfume_ownership", prompt);
	}

	[Theory]
	[InlineData(0, 1)]
	[InlineData(-5, 1)]
	[InlineData(7, 7)]
	public async Task BuildAsync_ClampsMaxResultsIntoPrompt(int configured, int expected) {
		var prompt = await CreateBuilder(hasMarketplaceOffers: false, maxResultsPerToolCall: configured).BuildAsync(TestContext.Current.CancellationToken);

		Assert.Contains($"up to {expected} perfumes", prompt);
		Assert.Contains($"request {expected} results", prompt);
	}
}
