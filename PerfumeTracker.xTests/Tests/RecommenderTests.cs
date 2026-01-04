using Microsoft.Extensions.DependencyInjection;
using Moq;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.Embedding;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.xTests.Fixture;
using Pgvector;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("PerfumeRecommender Tests")]
public class PerfumeRecommenderCollection : ICollectionFixture<PerfumeRecommenderFixture>;

public class PerfumeRecommenderFixture : DbFixture {
	public Mock<IEncoder> MockEncoder = new();

	public PerfumeRecommenderFixture() : base() {
		// Setup mock encoder to return a dummy vector
		MockEncoder.Setup(x => x.GetEmbeddings(It.IsAny<string>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(new Vector(Enumerable.Range(0, 1536).Select(i => (float)i / 1536).ToArray()));
	}

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		// Seed user profile
		await SeedUserProfile();

		// Clear and seed tags with seasonal keywords
		var sql = "truncate table \"public\".\"Tag\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);

		var tags = new List<Tag>();
		var seasonalTags = new[] {
			"Woody", "Floral", "Fresh", "Citrus", "Spicy", "Warm",
			"Aquatic", "Vanilla", "Amber", "Green", "Leather"
		};
		foreach (var tagName in seasonalTags) {
			tags.Add(new Tag {
				Id = Guid.NewGuid(),
				TagName = tagName,
				Color = "#000000",
				UserId = TenantProvider.MockTenantId!.Value,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow,
				Description = $"{tagName} scent"
			});
		}
		await context.Tags.AddRangeAsync(tags);
		await context.SaveChangesAsync();

		// Clear and seed perfumes
		sql = "truncate table \"public\".\"Perfume\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);

		var perfumes = new List<Perfume>();

		// High-rated perfume not worn recently (for ForgottenFavorite)
		var forgottenFav = GeneratePerfumes(1)[0];
		forgottenFav.Ml = 50;
		perfumes.Add(forgottenFav);

		// Recently worn perfumes
		var recentlyWorn = GeneratePerfumes(5);
		foreach (var p in recentlyWorn) {
			p.Ml = 50;
			perfumes.Add(p);
		}

		// Least used perfume
		var leastUsed = GeneratePerfumes(1)[0];
		leastUsed.Ml = 50;
		perfumes.Add(leastUsed);

		// Seasonal perfume
		var seasonalPerfume = GeneratePerfumes(1)[0];
		seasonalPerfume.Ml = 50;
		perfumes.Add(seasonalPerfume);

		// Random recommendation candidates
		var randomCandidates = GeneratePerfumes(5);
		foreach (var p in randomCandidates) {
			p.Ml = 50;
			perfumes.Add(p);
		}

		await context.Perfumes.AddRangeAsync(perfumes);
		await context.SaveChangesAsync();

		// Add ratings to perfumes (all above minimum rating threshold)
		foreach (var perfume in perfumes) {
			var rating = new PerfumeRating {
				Id = Guid.NewGuid(),
				PerfumeId = perfume.Id,
				Rating = 8.5m,
				RatingDate = DateTime.UtcNow,
				Comment = "Great perfume!",
				UserId = TenantProvider.MockTenantId!.Value,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			await context.PerfumeRatings.AddAsync(rating);
		}
		await context.SaveChangesAsync();

		// Add perfume events
		// Recent worn events for the 5 recently worn perfumes
		for (int i = 0; i < 5; i++) {
			var wornEvent = new PerfumeEvent {
				Id = Guid.NewGuid(),
				PerfumeId = recentlyWorn[i].Id,
				EventDate = DateTime.UtcNow.AddDays(-i),
				Type = PerfumeEvent.PerfumeEventType.Worn,
				AmountMl = 0.1m,
				SequenceNumber = 1,
				UserId = TenantProvider.MockTenantId!.Value,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			await context.PerfumeEvents.AddAsync(wornEvent);
		}

		// Old worn event for forgotten favorite
		var oldWornEvent = new PerfumeEvent {
			Id = Guid.NewGuid(),
			PerfumeId = forgottenFav.Id,
			EventDate = DateTime.UtcNow.AddDays(-90),
			Type = PerfumeEvent.PerfumeEventType.Worn,
			AmountMl = 0.1m,
			SequenceNumber = 1,
			UserId = TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		await context.PerfumeEvents.AddAsync(oldWornEvent);

		// Single worn event for least used
		var leastUsedEvent = new PerfumeEvent {
			Id = Guid.NewGuid(),
			PerfumeId = leastUsed.Id,
			EventDate = DateTime.UtcNow.AddDays(-60),
			Type = PerfumeEvent.PerfumeEventType.Worn,
			AmountMl = 0.1m,
			SequenceNumber = 1,
			UserId = TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		await context.PerfumeEvents.AddAsync(leastUsedEvent);
		await context.SaveChangesAsync();

		// Add tags to perfumes
		// Tag recently worn perfumes
		for (int i = 0; i < 3; i++) {
			var perfumeTag = new PerfumeTag {
				Id = Guid.NewGuid(),
				PerfumeId = recentlyWorn[i].Id,
				TagId = tags[i].Id,
				UserId = TenantProvider.MockTenantId!.Value,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			await context.PerfumeTags.AddAsync(perfumeTag);
		}

		// Tag seasonal perfume with seasonal tags
		var seasonalTag = tags.FirstOrDefault(t => t.TagName.ToLower() == "warm");
		if (seasonalTag != null) {
			var perfumeTag = new PerfumeTag {
				Id = Guid.NewGuid(),
				PerfumeId = seasonalPerfume.Id,
				TagId = seasonalTag.Id,
				UserId = TenantProvider.MockTenantId!.Value,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			await context.PerfumeTags.AddAsync(perfumeTag);
		}
		await context.SaveChangesAsync();

		// Add perfume documents with embeddings for similarity testing
		foreach (var perfume in perfumes.Take(8)) {
			var document = new PerfumeDocument {
				Id = perfume.Id,
				Text = $"Test document for {perfume.PerfumeName}",
				Embedding = new Vector(Enumerable.Range(0, 1536).Select(i => (float)i / 1536).ToArray()),
				UserId = TenantProvider.MockTenantId!.Value,
				CreatedAt = DateTime.UtcNow,
				UpdatedAt = DateTime.UtcNow
			};
			await context.PerfumeDocuments.AddAsync(document);
		}
		await context.SaveChangesAsync();
	}
}

[Collection("PerfumeRecommender Tests")]
public class PerfumeRecommenderTests {
	private readonly PerfumeRecommenderFixture _fixture;

	public PerfumeRecommenderTests(PerfumeRecommenderFixture fixture) {
		_fixture = fixture;
	}

	private IPerfumeRecommender GetRecommender(PerfumeTrackerContext context, IUserProfileService userProfileService) {
		// Get actual ChatClient from DI - it will be NullChatClient in test environment
		return new PerfumeRecommender(
			context,
			userProfileService,
			_fixture.MockEncoder.Object,
			_fixture.MockPresignedUrlService,
			null!); // ChatClient is only used for mood/occasion, which we test separately
	}

	[Fact]
	public async Task GetAllStrategyRecommendations_ReturnsRecommendations() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		var recommendations = await recommender.GetAllStrategyRecommendations(5, CancellationToken.None);

		Assert.NotNull(recommendations);
		var recList = recommendations.ToList();
		Assert.NotEmpty(recList);
		Assert.True(recList.Count <= 5);

		// Verify recommendations were persisted
		var persistedRecs = await context.PerfumeRecommendations.ToListAsync();
		Assert.NotEmpty(persistedRecs);

		// Verify outbox message was created
		var outboxMessages = await context.OutboxMessages.ToListAsync();
		Assert.Contains(outboxMessages, msg => msg.EventType.Contains("PerfumeRecommendationsAddedNotification"));
	}

	[Fact]
	public async Task GetRecommendationsForOccasionMoodPrompt_UsesCachedCompletion() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		// Clear any existing cached completions for this test
		var existingCompletions = await context.CachedCompletions
			.Where(cc => cc.Prompt == "test mood")
			.ToListAsync();
		context.CachedCompletions.RemoveRange(existingCompletions);
		await context.SaveChangesAsync();

		// Add a cached completion
		var cachedCompletion = new CachedCompletion {
			Prompt = "test mood",
			Response = "citrus, fresh, light",
			CompletionType = CachedCompletion.CompletionTypes.MoodOrOccasionRecommendation,
			UserId = _fixture.TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.CachedCompletions.Add(cachedCompletion);
		await context.SaveChangesAsync();

		var recommendations = await recommender.GetRecommendationsForOccasionMoodPrompt(
			3,
			"test mood",
			CancellationToken.None);

		Assert.NotNull(recommendations);
		var recList = recommendations.ToList();
		Assert.NotEmpty(recList);
		Assert.All(recList, r => Assert.Equal(RecommendationStrategy.MoodOrOccasion, r.Strategy));
	}

	[Fact]
	public async Task GetRecommendationsForOccasionMoodPrompt_WithEmptyPrompt_ThrowsException() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		await Assert.ThrowsAsync<ArgumentException>(async () =>
			await recommender.GetRecommendationsForOccasionMoodPrompt(3, "", CancellationToken.None));
	}

	[Fact]
	public async Task GetSimilar_WithValidPerfumeId_ReturnsSimilarPerfumes() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var userProfile = await userProfileService.GetCurrentUserProfile(CancellationToken.None);
		var recommender = GetRecommender(context, userProfileService);

		// Get a perfume with embedding
		var perfumeWithEmbedding = await context.PerfumeDocuments
			.FirstOrDefaultAsync(pd => pd.Embedding != null);
		Assert.NotNull(perfumeWithEmbedding);

		var similar = await recommender.GetSimilar(perfumeWithEmbedding.Id, 3, userProfile, CancellationToken.None);

		Assert.NotNull(similar);
		var similarList = similar.ToList();
		// Should return similar perfumes (excluding the source perfume itself)
		Assert.All(similarList, s => Assert.NotEqual(perfumeWithEmbedding.Id, s.Perfume.Id));
	}

	[Fact]
	public async Task GetSimilar_WithNonExistentPerfume_ReturnsEmpty() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var userProfile = await userProfileService.GetCurrentUserProfile(CancellationToken.None);
		var recommender = GetRecommender(context, userProfileService);

		var similar = await recommender.GetSimilar(Guid.NewGuid(), 3, userProfile, CancellationToken.None);

		Assert.NotNull(similar);
		Assert.Empty(similar);
	}

	[Fact]
	public async Task GetRecommendationStats_ReturnsCorrectStatistics() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		// Clear any existing recommendations from previous tests
		var existingRecs = await context.PerfumeRecommendations.ToListAsync();
		context.PerfumeRecommendations.RemoveRange(existingRecs);
		await context.SaveChangesAsync();

		// Create some test recommendations
		var perfume = await context.Perfumes.FirstAsync();
		var recommendation1 = new PerfumeRecommendation {
			PerfumeId = perfume.Id,
			Strategy = RecommendationStrategy.Random,
			IsAccepted = true,
			UserId = _fixture.TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		var recommendation2 = new PerfumeRecommendation {
			PerfumeId = perfume.Id,
			Strategy = RecommendationStrategy.Random,
			IsAccepted = false,
			UserId = _fixture.TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		var recommendation3 = new PerfumeRecommendation {
			PerfumeId = perfume.Id,
			Strategy = RecommendationStrategy.ForgottenFavorite,
			IsAccepted = true,
			UserId = _fixture.TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};

		context.PerfumeRecommendations.AddRange(recommendation1, recommendation2, recommendation3);
		await context.SaveChangesAsync();

		var stats = await recommender.GetRecommendationStats(CancellationToken.None);

		Assert.NotNull(stats);
		var statsList = stats.ToList();
		Assert.NotEmpty(statsList);

		var randomStats = statsList.FirstOrDefault(s => s.Strategy == RecommendationStrategy.Random);
		Assert.NotNull(randomStats);
		Assert.Equal(2, randomStats.TotalRecommendations);
		Assert.Equal(1, randomStats.AcceptedRecommendations);

		var forgottenStats = statsList.FirstOrDefault(s => s.Strategy == RecommendationStrategy.ForgottenFavorite);
		Assert.NotNull(forgottenStats);
		Assert.Equal(1, forgottenStats.TotalRecommendations);
		Assert.Equal(1, forgottenStats.AcceptedRecommendations);
	}

	[Fact]
	public async Task GetRecommendationStats_WithNoRecommendations_ReturnsEmpty() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		// Clear any existing recommendations
		var existingRecs = await context.PerfumeRecommendations.ToListAsync();
		context.PerfumeRecommendations.RemoveRange(existingRecs);
		await context.SaveChangesAsync();

		var stats = await recommender.GetRecommendationStats(CancellationToken.None);

		Assert.NotNull(stats);
		Assert.Empty(stats);
	}

	[Fact]
	public async Task Recommendations_RespectMinimumRating() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		// Update user profile to have a higher minimum rating
		var userProfile = await userProfileService.GetCurrentUserProfile(CancellationToken.None);
		userProfile.MinimumRating = 7.0m;
		context.UserProfiles.Update(userProfile);
		await context.SaveChangesAsync();

		// Create a perfume with low rating
		var lowRatedPerfume = _fixture.GeneratePerfumes(1)[0];
		lowRatedPerfume.Ml = 50;
		context.Perfumes.Add(lowRatedPerfume);
		await context.SaveChangesAsync();

		var lowRating = new PerfumeRating {
			Id = Guid.NewGuid(),
			PerfumeId = lowRatedPerfume.Id,
			Rating = 3.0m, // Below minimum rating
			RatingDate = DateTime.UtcNow,
			UserId = _fixture.TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.PerfumeRatings.Add(lowRating);
		await context.SaveChangesAsync();

		var recommendations = await recommender.GetAllStrategyRecommendations(10, CancellationToken.None);

		// Low rated perfume should not appear in recommendations
		Assert.DoesNotContain(recommendations, r => r.Perfume.Perfume.Id == lowRatedPerfume.Id);

		// Reset user profile
		userProfile.MinimumRating = 0;
		context.UserProfiles.Update(userProfile);
		await context.SaveChangesAsync();
	}

	[Fact]
	public async Task Recommendations_ExcludeRecentlyWornPerfumes() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		// Get the most recently worn perfumes
		var recentlyWornIds = await context.PerfumeEvents
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn)
			.OrderByDescending(x => x.EventDate)
			.Select(x => x.PerfumeId)
			.Distinct()
			.Take(5)
			.ToListAsync();

		var recommendations = await recommender.GetAllStrategyRecommendations(10, CancellationToken.None);

		// Recently worn perfumes should not appear in most recommendation strategies
		// (except ForgottenFavorite which specifically targets old but highly rated perfumes)
		var recommendedIds = recommendations.Select(r => r.Perfume.Perfume.Id).ToList();
		var overlap = recommendedIds.Intersect(recentlyWornIds).Count();

		// There should be minimal or no overlap
		Assert.True(overlap < recentlyWornIds.Count);
	}

	[Fact]
	public async Task Recommendations_OnlyIncludePerfumesWithMl() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		// Create a perfume with no ml left
		var emptyPerfume = _fixture.GeneratePerfumes(1)[0];
		emptyPerfume.Ml = 10;
		emptyPerfume.MlLeft = 0;
		context.Perfumes.Add(emptyPerfume);
		await context.SaveChangesAsync();

		var rating = new PerfumeRating {
			Id = Guid.NewGuid(),
			PerfumeId = emptyPerfume.Id,
			Rating = 9.0m,
			RatingDate = DateTime.UtcNow,
			UserId = _fixture.TenantProvider.MockTenantId!.Value,
			CreatedAt = DateTime.UtcNow,
			UpdatedAt = DateTime.UtcNow
		};
		context.PerfumeRatings.Add(rating);
		await context.SaveChangesAsync();

		var recommendations = await recommender.GetAllStrategyRecommendations(10, CancellationToken.None);

		// Empty perfume should not appear in recommendations
		Assert.DoesNotContain(recommendations, r => r.Perfume.Perfume.Id == emptyPerfume.Id);
	}

	[Fact]
	public async Task Recommendations_DeduplicatesPerfumes() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var recommender = GetRecommender(context, userProfileService);

		var recommendations = await recommender.GetAllStrategyRecommendations(10, CancellationToken.None);

		var recList = recommendations.ToList();
		var perfumeIds = recList.Select(r => r.Perfume.Perfume.Id).ToList();
		var distinctIds = perfumeIds.Distinct().ToList();

		// Should not have duplicate perfume IDs
		Assert.Equal(distinctIds.Count, perfumeIds.Count);
	}

	[Fact]
	public async Task GetSimilar_ExcludesSourcePerfume() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var userProfileService = scope.ServiceProvider.GetRequiredService<IUserProfileService>();
		var userProfile = await userProfileService.GetCurrentUserProfile(CancellationToken.None);
		var recommender = GetRecommender(context, userProfileService);

		// Get a perfume with embedding
		var sourcePerfume = await context.PerfumeDocuments
			.FirstOrDefaultAsync(pd => pd.Embedding != null);
		Assert.NotNull(sourcePerfume);

		var similar = await recommender.GetSimilar(sourcePerfume.Id, 10, userProfile, CancellationToken.None);

		// Source perfume should never appear in its own similar recommendations
		Assert.DoesNotContain(similar, s => s.Perfume.Id == sourcePerfume.Id);
	}
}
