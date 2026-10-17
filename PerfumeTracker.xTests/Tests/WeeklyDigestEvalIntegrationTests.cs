using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Evals.WeeklyDigests;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.xTests.Tests;

[Collection("Weekly digest database")]
public class WeeklyDigestEvalIntegrationTests(WeeklyDigestDatabase fixture) {
	[Fact]
	public async Task CandidateQueries_ExcludeAllDecoysAndPreserveOtherFixtureTenants() {
		using var scope = fixture.App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var other = DigestEvalFixtures.Build(new() { Id = "integration-other", Fixture = "tiny" });
		db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(other.UserId);
		await DigestEvalFixtures.SeedAsync(db, other, TestContext.Current.CancellationToken);
		var definition = DigestEvalFixtures.Build(new() { Id = "integration-eligibility", Fixture = "eligibility" });
		db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(definition.UserId);
		await DigestEvalFixtures.SeedAsync(db, definition, TestContext.Current.CancellationToken);
		var source = new DigestCandidates(db, new FrozenDigestTime(definition.Now));
		var page = await source.Get(RecommendationStrategy.LeastUsed, 0, 60, null, TestContext.Current.CancellationToken);
		Assert.Equal(8, page.Total);
		Assert.Equal(8, page.Perfumes.Count);
		Assert.DoesNotContain(page.Perfumes, p => p.House == "Decoy House");
		Assert.Equal(2, await db.Perfumes.IgnoreQueryFilters().CountAsync(p => p.UserId == other.UserId, TestContext.Current.CancellationToken));
	}

	[Theory]
	[InlineData(1, "Winter")]
	[InlineData(7, "Summer")]
	public async Task CandidateQueries_UseFrozenSeasonAndStableWeekPaging(int month, string season) {
		using var scope = fixture.App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var definition = DigestEvalFixtures.Build(new() { Id = $"integration-season-{month}", Now = new(2026, month, 12, 12, 0, 0, TimeSpan.Zero) });
		db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(definition.UserId);
		await DigestEvalFixtures.SeedAsync(db, definition, TestContext.Current.CancellationToken);
		var source = new DigestCandidates(db, new FrozenDigestTime(definition.Now));
		var context = JsonSerializer.SerializeToElement(await source.GetContext(definition.WeekStart, TestContext.Current.CancellationToken));
		Assert.Equal(season, context.GetProperty("season").GetString());
		var seasonal = await source.Get(RecommendationStrategy.Seasonal, 0, 6, null, TestContext.Current.CancellationToken);
		Assert.All(seasonal.Perfumes, p => Assert.Contains(p.Tags, t => month == 1 ? t is "amber" or "vanilla" : t is "marine" or "fresh"));
		var first = await source.Get(RecommendationStrategy.Random, 0, 6, null, TestContext.Current.CancellationToken);
		var second = await source.Get(RecommendationStrategy.Random, 6, 6, null, TestContext.Current.CancellationToken);
		var whole = await source.Get(RecommendationStrategy.Random, 0, 12, null, TestContext.Current.CancellationToken);
		Assert.Equal(whole.Perfumes.Select(p => p.Id), first.Perfumes.Concat(second.Perfumes).Select(p => p.Id));
		Assert.Equal(12, whole.Perfumes.Select(p => p.Id).Distinct().Count());
		var nextWeek = await new DigestCandidates(db, new FrozenDigestTime(definition.Now.AddDays(7)))
			.Get(RecommendationStrategy.Random, 0, 12, null, TestContext.Current.CancellationToken);
		Assert.NotEqual(whole.Perfumes.Select(p => p.Id).ToArray(), nextWeek.Perfumes.Select(p => p.Id).ToArray());
	}

	[Fact]
	public async Task MissingEmbeddings_UsesRatingFallback_AndHistoryUsesFourFixedWeeks() {
		using var scope = fixture.App.Services.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var definition = DigestEvalFixtures.Build(new() { Id = "integration-no-embeddings", Fixture = "no-embeddings" });
		db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(definition.UserId);
		await DigestEvalFixtures.SeedAsync(db, definition, TestContext.Current.CancellationToken);
		var source = new DigestCandidates(db, new FrozenDigestTime(definition.Now));
		var page = await source.Get(RecommendationStrategy.SimilarToLastUsed, 0, 60, null, TestContext.Current.CancellationToken);
		Assert.Equal(page.Perfumes.OrderByDescending(p => p.Rating).Select(p => p.Rating), page.Perfumes.Select(p => p.Rating));
		var history = DigestEvalFixtures.Build(new() { Id = "integration-history", Fixture = "history" });
		db.TenantProvider = new WeeklyDigestDatabase.FixedTenant(history.UserId);
		await DigestEvalFixtures.SeedAsync(db, history, TestContext.Current.CancellationToken);
		var context = JsonSerializer.SerializeToElement(await new DigestCandidates(db, new FrozenDigestTime(history.Now)).GetContext(history.WeekStart, TestContext.Current.CancellationToken));
		Assert.Equal(4, context.GetProperty("previousDigests").GetArrayLength());
	}
}
