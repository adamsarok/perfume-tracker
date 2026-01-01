using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Common.Services;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Services.Missions;
using PerfumeTracker.xTests.Fixture;
using static PerfumeTracker.Server.Features.Perfumes.Services.PerfumeRecommender;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Mission Tests")]
public class MissionCollection : ICollectionFixture<MissionFixture>;

public class MissionFixture : DbFixture {
	public MissionFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"public\".\"Mission\" CASCADE; TRUNCATE TABLE \"public\".\"UserMission\" CASCADE;");
		var now = DateTime.UtcNow;
		foreach (MissionType type in Enum.GetValues(typeof(MissionType))) {
			var mission = new Mission {
				Id = Guid.NewGuid(),
				Name = $"Test Mission {type}",
				Description = $"Test Desc {type}",
				StartDate = now.AddDays(-1),
				EndDate = now.AddDays(1),
				IsActive = true,
				Type = type,
				RequiredCount = 1,
				XP = 10
			};
			var userMission = new UserMission {
				Mission = mission,
				Id = Guid.NewGuid(),
				UserId = TenantProvider.MockTenantId ?? throw new TenantNotSetException()
			};
			context.Missions.Add(mission);
			context.UserMissions.Add(userMission);
		}
		await context.SaveChangesAsync();
	}
}

[Collection("Mission Tests")]
public class MissionTests {
	private readonly MissionFixture _fixture;

	public MissionTests(MissionFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task GenerateMissionsHandler_CreatesUserMissions() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var handler = new GenerateMissions(context);
		await handler.Handle(new GenerateMissionCommand(), CancellationToken.None);
		var userMissions = await context.UserMissions.ToListAsync();
		Assert.NotEmpty(userMissions);
	}

	[Fact]
	public async Task GetActiveMissionsHandler_ReturnsActiveMissions() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var handlerc = new GenerateMissions(context);
		await handlerc.Handle(new GenerateMissionCommand(), CancellationToken.None);
		var handler = new GetActiveMissionsHandler(context);
		var result = await handler.Handle(new GetActiveMissionsQuery(), CancellationToken.None);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	public async Task ProgressMissions_PerfumeEventNotificationHandler_UpdatesProgress() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var xpService = scope.ServiceProvider.GetRequiredService<IXPService>();
		var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(context, _fixture.MockMissionProgressHubContext.Object, xpService);
		var handler = new ProgressMissions.PerfumeEventNotificationHandler(context, updateHandler);
		var notification = new PerfumeEventAddedNotification(Guid.NewGuid(), Guid.NewGuid(), _fixture.TenantProvider.MockTenantId ?? throw new TenantNotSetException(), PerfumeEvent.PerfumeEventType.Worn);
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.WearDifferentPerfumes);
	}

	[Fact]
	public async Task ProgressMissions_PerfumeRandomAcceptedNotificationHandler_UpdatesProgress() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var xpService = scope.ServiceProvider.GetRequiredService<IXPService>();

		var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(context, _fixture.MockMissionProgressHubContext.Object, xpService);
		var handler = new ProgressMissions.PerfumeRecommendationAcceptedNotificationHandler(updateHandler);
		var notification = new PerfumeRecommendationAcceptedNotification(Guid.NewGuid(), _fixture.TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.AcceptRecommendations);
	}

	[Fact]
	public async Task ProgressMissions_RandomPerfumeAddedNotificationHandler_UpdatesProgress() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var xpService = scope.ServiceProvider.GetRequiredService<IXPService>();
		var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(context, _fixture.MockMissionProgressHubContext.Object, xpService);
		var handler = new ProgressMissions.PerfumeRecommendationsAddedNotificationHandler(updateHandler);
		var notification = new PerfumeRecommendationsAddedNotification(1, _fixture.TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.GetRecommendations);
	}

	void AssertProgress(MissionType type) {
		Assert.NotNull(_fixture.HubMessages);
		Assert.NotEmpty(_fixture.HubMessages);
		foreach (var m in _fixture.HubMessages) {
			foreach (var s in m.HubSentArgs) {
				var mission = s as UserMissionDto;
				if (mission != null && mission.MissionType == type) {
					Assert.NotNull(mission);
					Assert.True(mission.Progress > 0);
					_fixture.HubMessages.Clear();
					return;
				}
			}
		}
		throw new Exception("Correct mission type not found in update args");
	}
}