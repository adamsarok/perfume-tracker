using Bogus;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;
using PerfumeTracker.Server.Services.Outbox;
using static PerfumeTracker.Server.Services.Missions.ProgressMissions;
using static PerfumeTracker.Server.Services.Streaks.ProgressStreaks;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace PerfumeTracker.xTests.Fixture;

public abstract class DbFixture : IAsyncLifetime {
	public WebApplicationFactory<Program> Factory { get; }
	public MockTenantProvider TenantProvider = new();
	public Mock<IHubContext<MissionProgressHub>> MockMissionProgressHubContext;
	public Mock<IHubContext<StreakProgressHub>> MockStreakProgressHubContext;
	public Mock<IClientProxy> MockClientProxy;
	public List<HubMessage> HubMessages = [];
	public Mock<ISideEffectQueue> MockSideEffectQueue;
	public MockPresignedUrlService MockPresignedUrlService = new();
	public record HubMessage(string HubSentMethod, object[] HubSentArgs);

	// Base Faker
	private readonly Faker _faker = new Faker();

	// Faker instances for all models
	public Faker<Perfume> PerfumeFaker { get; }
	public Faker<Tag> TagFaker { get; }
	public Faker<PerfumeTag> PerfumeTagFaker { get; }
	public Faker<PerfumeEvent> PerfumeEventFaker { get; }
	public Faker<PerfumeRating> PerfumeRatingFaker { get; }
	public Faker<PerfumeRandoms> PerfumeRandomsFaker { get; }
	public Faker<Recommendation> RecommendationFaker { get; }
	public Faker<Achievement> AchievementFaker { get; }
	public Faker<UserAchievement> UserAchievementFaker { get; }
	public Faker<Mission> MissionFaker { get; }
	public Faker<UserMission> UserMissionFaker { get; }
	public Faker<UserStreak> UserStreakFaker { get; }
	public Faker<UserProfile> UserProfileFaker { get; }
	//public Faker<OutboxMessage> OutboxMessageFaker { get; }
	public Faker<Invite> InviteFaker { get; }

	public DbFixture() {
		Factory = new WebApplicationFactory<Program>()
			  .WithWebHostBuilder(builder => {
				  builder.UseEnvironment("Test");
				  builder.ConfigureServices(services => {
					  var descriptor = services.SingleOrDefault(
						  d => d.ServiceType == typeof(ITenantProvider));
					  if (descriptor != null) services.Remove(descriptor);
					  services.AddScoped<ITenantProvider>(_ => TenantProvider);

					  descriptor = services.SingleOrDefault(
							d => d.ServiceType == typeof(IPresignedUrlService));
					  if (descriptor != null) services.Remove(descriptor);
					  services.AddScoped<IPresignedUrlService>(_ => MockPresignedUrlService);
				  });
			  });

		//using var scope = GetTestScope();
		var logger = Mock.Of<ILogger<CreateUser>>();

		var mockClients = new Mock<IHubClients>();
		MockClientProxy = new Mock<IClientProxy>();
		_ = mockClients.Setup(clients => clients.All).Returns(MockClientProxy.Object);
		_ = MockClientProxy.Setup(proxy => proxy
			.SendCoreAsync(It.IsAny<string>(), It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask)
			.Callback<string, object[], CancellationToken>((method, args, token) =>
				HubMessages.Add(new HubMessage(method, args)));

		MockMissionProgressHubContext = new Mock<IHubContext<MissionProgressHub>>();
		_ = MockMissionProgressHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

		MockStreakProgressHubContext = new Mock<IHubContext<StreakProgressHub>>();
		_ = MockStreakProgressHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

		var userProxies = new Dictionary<string, Mock<IClientProxy>>();
		_ = mockClients.Setup(clients => clients.User(It.IsAny<string>())).Returns((string userId) => {
			if (!userProxies.ContainsKey(userId)) userProxies[userId] = MockClientProxy;
			return userProxies[userId].Object;
		});
		MockSideEffectQueue = new Mock<ISideEffectQueue>();

		var tenantId = TenantProvider.GetCurrentUserId();

		// Initialize all Fakers
		PerfumeFaker = new Faker<Perfume>()
			.RuleFor(p => p.Id, f => Guid.NewGuid())
			.RuleFor(p => p.House, f => f.Company.CompanyName())
			.RuleFor(p => p.PerfumeName, f => f.Commerce.ProductName())
			.RuleFor(p => p.Ml, f => f.PickRandom(new[] { 5m, 10m, 30m, 50m, 75m, 100m }))
			.RuleFor(p => p.MlLeft, (f, p) => f.Random.Decimal(0, p.Ml))
			.RuleFor(p => p.ImageObjectKeyNew, f => f.Random.Bool(0.3f) ? Guid.NewGuid() : null)
			.RuleFor(p => p.Autumn, f => f.Random.Bool())
			.RuleFor(p => p.Spring, f => f.Random.Bool())
			.RuleFor(p => p.Summer, f => f.Random.Bool())
			.RuleFor(p => p.Winter, f => f.Random.Bool())
			.RuleFor(p => p.UserId, tenantId)
			.RuleFor(p => p.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(p => p.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(p => p.IsDeleted, false);

		TagFaker = new Faker<Tag>()
			.RuleFor(t => t.Id, f => Guid.NewGuid())
			.RuleFor(t => t.TagName, f => $"{f.PickRandom(new[] {
				"Woody", "Floral", "Fresh", "Citrus", "Spicy", "Oriental",
				"Aquatic", "Fruity", "Green", "Leather", "Gourmand", "Aromatic",
				"Powdery", "Musky", "Amber", "Vanilla", "Rose", "Jasmine"
			})}_{Guid.NewGuid().ToString().Substring(0, 8)}")
			.RuleFor(t => t.Color, f => f.Internet.Color())
			.RuleFor(t => t.UserId, tenantId)
			.RuleFor(t => t.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(t => t.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(t => t.IsDeleted, false);

		PerfumeTagFaker = new Faker<PerfumeTag>()
			.RuleFor(pt => pt.Id, f => Guid.NewGuid())
			.RuleFor(pt => pt.PerfumeId, f => Guid.NewGuid())
			.RuleFor(pt => pt.TagId, f => Guid.NewGuid())
			.RuleFor(pt => pt.UserId, tenantId)
			.RuleFor(pt => pt.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(pt => pt.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(pt => pt.IsDeleted, false);

		PerfumeEventFaker = new Faker<PerfumeEvent>()
			.RuleFor(pe => pe.Id, f => Guid.NewGuid())
			.RuleFor(pe => pe.PerfumeId, f => Guid.NewGuid())
			.RuleFor(pe => pe.EventDate, f => f.Date.RecentOffset(30).UtcDateTime)
			.RuleFor(pe => pe.Type, f => f.PickRandom<PerfumeEvent.PerfumeEventType>())
			.RuleFor(pe => pe.AmountMl, f => f.Random.Decimal(0.05m, 0.5m))
			.RuleFor(pe => pe.SequenceNumber, f => f.Random.Int(1, 1000))
			.RuleFor(pe => pe.UserId, tenantId)
			.RuleFor(pe => pe.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(pe => pe.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(pe => pe.IsDeleted, false);

		PerfumeRatingFaker = new Faker<PerfumeRating>()
			.RuleFor(pr => pr.Id, f => Guid.NewGuid())
			.RuleFor(pr => pr.PerfumeId, f => Guid.NewGuid())
			.RuleFor(pr => pr.RatingDate, f => f.Date.RecentOffset(90).UtcDateTime)
			.RuleFor(pr => pr.Rating, f => f.Random.Decimal(1, 10))
			.RuleFor(pr => pr.Comment, f => f.Lorem.Sentence(10))
			.RuleFor(pr => pr.UserId, tenantId)
			.RuleFor(pr => pr.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(pr => pr.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(pr => pr.IsDeleted, false);

		PerfumeRandomsFaker = new Faker<PerfumeRandoms>()
			.RuleFor(pr => pr.Id, f => Guid.NewGuid())
			.RuleFor(pr => pr.PerfumeId, f => Guid.NewGuid())
			.RuleFor(pr => pr.IsAccepted, f => f.Random.Bool(0.3f))
			.RuleFor(pr => pr.UserId, tenantId)
			.RuleFor(pr => pr.CreatedAt, f => f.Date.RecentOffset(30).UtcDateTime)
			.RuleFor(pr => pr.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(pr => pr.IsDeleted, false);

		RecommendationFaker = new Faker<Recommendation>()
			.RuleFor(r => r.Id, f => Guid.NewGuid())
			.RuleFor(r => r.Query, f => f.Lorem.Sentence(5))
			.RuleFor(r => r.Recommendations, f => f.Lorem.Paragraph())
			.RuleFor(r => r.UserId, tenantId)
			.RuleFor(r => r.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(r => r.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(r => r.IsDeleted, false);

		AchievementFaker = new Faker<Achievement>()
			.RuleFor(a => a.Id, f => Guid.NewGuid())
			.RuleFor(a => a.Name, f => f.Lorem.Word())
			.RuleFor(a => a.Description, f => f.Lorem.Sentence())
			.RuleFor(a => a.MinPerfumesAdded, f => f.Random.Bool() ? f.Random.Int(1, 100) : null)
			.RuleFor(a => a.MinPerfumeWornDays, f => f.Random.Bool() ? f.Random.Int(1, 365) : null)
			.RuleFor(a => a.MinTags, f => f.Random.Bool() ? f.Random.Int(1, 50) : null)
			.RuleFor(a => a.MinPerfumeTags, f => f.Random.Bool() ? f.Random.Int(1, 100) : null)
			.RuleFor(a => a.MinStreak, f => f.Random.Bool() ? f.Random.Int(1, 100) : null)
			.RuleFor(a => a.MinXP, f => f.Random.Bool() ? f.Random.Int(100, 10000) : null)
			.RuleFor(a => a.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(a => a.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(a => a.IsDeleted, false);

		UserAchievementFaker = new Faker<UserAchievement>()
			.RuleFor(ua => ua.Id, f => Guid.NewGuid())
			.RuleFor(ua => ua.AchievementId, f => Guid.NewGuid())
			.RuleFor(ua => ua.IsRead, f => f.Random.Bool(0.7f))
			.RuleFor(ua => ua.UserId, tenantId)
			.RuleFor(ua => ua.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(ua => ua.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(ua => ua.IsDeleted, false);

		MissionFaker = new Faker<Mission>()
			.RuleFor(m => m.Id, f => Guid.NewGuid())
			.RuleFor(m => m.Name, f => f.Lorem.Word())
			.RuleFor(m => m.Description, f => f.Lorem.Sentence())
			.RuleFor(m => m.StartDate, f => f.Date.RecentOffset(7).UtcDateTime)
			.RuleFor(m => m.EndDate, f => f.Date.SoonOffset(7).UtcDateTime)
			.RuleFor(m => m.XP, f => f.Random.Int(10, 200))
			.RuleFor(m => m.Type, f => f.PickRandom<MissionType>())
			.RuleFor(m => m.RequiredCount, f => f.Random.Int(1, 10))
			.RuleFor(m => m.RequiredId, f => f.Random.Bool(0.2f) ? Guid.NewGuid() : null)
			.RuleFor(m => m.IsActive, f => f.Random.Bool(0.8f))
			.RuleFor(m => m.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(m => m.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(m => m.IsDeleted, false)
			.Ignore(m => m.UserMissions);

		UserMissionFaker = new Faker<UserMission>()
			.RuleFor(um => um.Id, f => Guid.NewGuid())
			.RuleFor(um => um.MissionId, f => Guid.NewGuid())
			.RuleFor(um => um.Progress, f => f.Random.Int(0, 10))
			.RuleFor(um => um.IsCompleted, f => f.Random.Bool(0.3f))
			.RuleFor(um => um.CompletedAt, (f, um) => um.IsCompleted ? f.Date.RecentOffset(7).UtcDateTime : null)
			.RuleFor(um => um.XP_Awarded, (f, um) => um.IsCompleted ? f.Random.Int(10, 200) : 0)
			.RuleFor(um => um.UserId, tenantId)
			.RuleFor(um => um.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(um => um.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(um => um.IsDeleted, false)
			.Ignore(um => um.User)
			.Ignore(um => um.Mission);

		UserStreakFaker = new Faker<UserStreak>()
			.RuleFor(us => us.Id, f => Guid.NewGuid())
			.RuleFor(us => us.StreakStartAt, f => f.Date.PastOffset(30).UtcDateTime)
			.RuleFor(us => us.LastProgressedAt, f => f.Date.RecentOffset(1).UtcDateTime)
			.RuleFor(us => us.StreakEndAt, f => f.Random.Bool(0.2f) ? f.Date.RecentOffset(7).UtcDateTime : null)
			.RuleFor(us => us.Progress, f => f.Random.Int(1, 100))
			.RuleFor(us => us.UserId, tenantId)
			.RuleFor(us => us.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(us => us.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(us => us.IsDeleted, false);

		UserProfileFaker = new Faker<UserProfile>()
			.RuleFor(up => up.Id, tenantId)
			.RuleFor(up => up.MinimumRating, f => f.Random.Decimal(0, 10))
			.RuleFor(up => up.DayFilter, f => f.Random.Int(7, 90))
			.RuleFor(up => up.ShowMalePerfumes, f => f.Random.Bool(0.8f))
			.RuleFor(up => up.ShowUnisexPerfumes, f => f.Random.Bool(0.8f))
			.RuleFor(up => up.ShowFemalePerfumes, f => f.Random.Bool(0.8f))
			.RuleFor(up => up.SprayAmountFullSizeMl, f => f.Random.Decimal(0.1m, 0.3m))
			.RuleFor(up => up.SprayAmountSamplesMl, f => f.Random.Decimal(0.05m, 0.15m))
			.RuleFor(up => up.Timezone, f => f.PickRandom(new[] { "UTC", "America/New_York", "Europe/London", "Asia/Tokyo" }))
			.RuleFor(up => up.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(up => up.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(up => up.IsDeleted, false);

		InviteFaker = new Faker<Invite>()
			.RuleFor(i => i.Id, f => Guid.NewGuid())
			.RuleFor(i => i.Email, f => f.Internet.Email())
			.RuleFor(i => i.IsUsed, f => f.Random.Bool(0.3f))
			.RuleFor(i => i.CreatedAt, f => f.Date.PastOffset(1).UtcDateTime)
			.RuleFor(i => i.UpdatedAt, f => DateTime.UtcNow)
			.RuleFor(i => i.IsDeleted, false);
	}

	public async Task InitializeAsync() {
		// Run once before all tests in this collection
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var connectionString = context.Database.GetConnectionString();
		var builder = new Npgsql.NpgsqlConnectionStringBuilder(connectionString);
		var databaseName = builder.Database ?? string.Empty;

		if (string.IsNullOrEmpty(databaseName)) throw new Exception("Test database not configured");
		if (!databaseName.Contains("test", StringComparison.OrdinalIgnoreCase)) throw new Exception("Connected database is not a test database");

		ILogger<CreateUser> logger = scope.ServiceProvider.GetRequiredService<ILogger<CreateUser>>();
		var userManager = scope.ServiceProvider.GetRequiredService<UserManager<PerfumeIdentityUser>>();
		const string testMail = "test@example.com";
		var user = await userManager.FindByEmailAsync(testMail);
		if (user == null) {
			var createUserHandler = new CreateUser(logger, userManager, context);
			user = createUserHandler.Create("test", "abcd1234ABCDxyz59697", Roles.USER, "test@example.com", false).GetAwaiter().GetResult();
		}
		if (user == null) throw new InvalidOperationException("User creation failed");
		TenantProvider.MockTenantId = user.Id;
		await SeedTestData(context);
	}

	public async Task DisposeAsync() {
		await Factory.DisposeAsync();
	}

	public abstract Task SeedTestData(PerfumeTrackerContext context);

	// Helper methods to generate data with specific UserId
	public List<Perfume> GeneratePerfumes(int count) {
		return PerfumeFaker.Clone()
			.RuleFor(p => p.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
	}

	public List<Tag> GenerateTags(int count) {
		return TagFaker.Clone()
			.RuleFor(t => t.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
	}

	public List<PerfumeEvent> GeneratePerfumeEvents(int count, Guid perfumeId) {
		return PerfumeEventFaker.Clone()
			.RuleFor(pe => pe.PerfumeId, perfumeId)
			.RuleFor(pe => pe.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
	}

	public List<PerfumeRating> GeneratePerfumeRatings(int count, Guid perfumeId) {
		return PerfumeRatingFaker.Clone()
			.RuleFor(pr => pr.PerfumeId, perfumeId)
			.RuleFor(pr => pr.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
	}

	public List<PerfumeTag> GeneratePerfumeTags(List<Guid> perfumeIds, List<Guid> tagIds) {
		var perfumeTags = new List<PerfumeTag>();
		foreach (var perfumeId in perfumeIds) {
			var selectedTags = _faker.PickRandom(tagIds, _faker.Random.Int(1, Math.Min(3, tagIds.Count)));
			foreach (var tagId in selectedTags) {
				perfumeTags.Add(new PerfumeTag {
					Id = Guid.NewGuid(),
					PerfumeId = perfumeId,
					TagId = tagId,
					UserId = TenantProvider.MockTenantId!.Value,
					CreatedAt = DateTime.UtcNow,
					UpdatedAt = DateTime.UtcNow
				});
			}
		}
		return perfumeTags;
	}

	public async Task<List<Tag>> SeedTags(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"Tag\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var tags = GenerateTags(count);
		await context.Tags.AddRangeAsync(tags);
		await context.SaveChangesAsync();
		return tags;
	}
	public async Task<List<Perfume>> SeedPerfumes(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"Perfume\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = GeneratePerfumes(count);
		await context.Perfumes.AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<PerfumeTag>> SeedPerfumeTags(List<Guid> perfumeIds, List<Guid> tagIds) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"PerfumeTag\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = GeneratePerfumeTags(perfumeIds, tagIds);
		await context.Set<PerfumeTag>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<PerfumeEvent>> SeedPerfumeEvents(int count, Guid perfumeId) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"PerfumeEvent\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = GeneratePerfumeEvents(count, perfumeId);
		await context.Set<PerfumeEvent>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<PerfumeRating>> SeedPerfumeRatings(int count, Guid perfumeId) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"PerfumeRating\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = GeneratePerfumeRatings(count, perfumeId);
		await context.Set<PerfumeRating>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<PerfumeRandoms>> SeedPerfumeRandoms(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"PerfumeRandom\" cascade;";
		var perfumes = GeneratePerfumes(count);
		await context.Perfumes.AddRangeAsync(perfumes);
		await context.SaveChangesAsync();
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = PerfumeRandomsFaker.Clone()
			.RuleFor(pr => pr.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
		for (int i = 0; i < count; i++) {
			result[i].PerfumeId = perfumes[i].Id;
		}
		await context.Set<PerfumeRandoms>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<Recommendation>> SeedRecommendations(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"Recommendation\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = RecommendationFaker.Clone()
			.RuleFor(r => r.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
		await context.Set<Recommendation>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<Achievement>> SeedAchievements(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"Achievement\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = AchievementFaker.Generate(count);
		await context.Set<Achievement>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<UserAchievement>> SeedUserAchievements(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"UserAchievement\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = UserAchievementFaker.Clone()
			.RuleFor(ua => ua.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
		await context.Set<UserAchievement>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<Mission>> SeedMissions(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"Mission\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = MissionFaker.Generate(count);
		await context.Set<Mission>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<UserMission>> SeedUserMissions(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"UserMission\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = UserMissionFaker.Clone()
			.RuleFor(um => um.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
		await context.Set<UserMission>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<UserStreak>> SeedUserStreaks(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"UserStreak\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = UserStreakFaker.Clone()
			.RuleFor(us => us.UserId, TenantProvider.MockTenantId!.Value)
			.Generate(count);
		await context.Set<UserStreak>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<UserProfile> SeedUserProfile() {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"UserProfile\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = UserProfileFaker.Clone()
			.RuleFor(up => up.Id, TenantProvider.MockTenantId!.Value)
			.Generate();
		await context.Set<UserProfile>().AddAsync(result);
		await context.SaveChangesAsync();
		return result;
	}

	public async Task<List<Invite>> SeedInvites(int count) {
		using var scope = Factory.Services.CreateScope();
		using var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var sql = "truncate table \"public\".\"Invite\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		var result = InviteFaker.Generate(count);
		await context.Set<Invite>().AddRangeAsync(result);
		await context.SaveChangesAsync();
		return result;
	}
}
