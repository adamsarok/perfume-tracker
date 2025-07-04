using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Models;
using Xunit;
using Microsoft.AspNetCore.Mvc.Testing;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Features.PerfumeRandoms;

namespace PerfumeTracker.xTests;

public class MissionTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public MissionTests(WebApplicationFactory<Program> factory) : base(factory) { }

	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

	private async Task PrepareMissionData() {
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = GetTestScope();
				await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"public\".\"Mission\" CASCADE; TRUNCATE TABLE \"public\".\"UserMission\" CASCADE;");
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
					scope.PerfumeTrackerContext.Missions.Add(mission);
					scope.PerfumeTrackerContext.UserMissions.Add(userMission);
				}
				await scope.PerfumeTrackerContext.SaveChangesAsync();
				dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	[Fact]
	public async Task GenerateMissionsHandler_CreatesUserMissions() {
		await PrepareMissionData();
		using var scope = GetTestScope();
		var handler = new GenerateMissions(scope.PerfumeTrackerContext);
		await handler.Handle(new GenerateMissionCommand(), CancellationToken.None);
		var userMissions = await scope.PerfumeTrackerContext.UserMissions.ToListAsync();
		Assert.NotEmpty(userMissions);
	}

	[Fact]
	public async Task GetActiveMissionsHandler_ReturnsActiveMissions() {
		await PrepareMissionData();
		using var scope = GetTestScope();
		var handlerc = new GenerateMissions(scope.PerfumeTrackerContext);
		await handlerc.Handle(new GenerateMissionCommand(), CancellationToken.None);
		var handler = new GetActiveMissionsHandler(scope.PerfumeTrackerContext);
		var result = await handler.Handle(new GetActiveMissionsQuery(), CancellationToken.None);
		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	public async Task ProgressMissions_PerfumeEventNotificationHandler_UpdatesProgress() {
		await PrepareMissionData();
		using var scope = GetTestScope();
		var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
		var handler = new ProgressMissions.PerfumeEventNotificationHandler(scope.PerfumeTrackerContext, updateHandler);
		var notification = new PerfumeEventAddedNotification(Guid.NewGuid(), Guid.NewGuid(), TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.WearPerfumes);
	}

	[Fact]
	public async Task ProgressMissions_PerfumeTagsAddedNotificationHandler_UpdatesProgress() {
		await PrepareMissionData();
		using var scope = GetTestScope();
		var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
		var handler = new ProgressMissions.PerfumeTagsAddedNotificationHandler(updateHandler);
		var notification = new PerfumeTagsAddedNotification(new List<Guid> { Guid.NewGuid() }, TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.PerfumesTaggedPhotographed);
	}

	[Fact]
	public async Task ProgressMissions_PerfumeRandomAcceptedNotificationHandler_UpdatesProgress() {
		await PrepareMissionData();
		using var scope = GetTestScope();
		var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
		var handler = new ProgressMissions.PerfumeRandomAcceptedNotificationHandler(updateHandler);
		var notification = new PerfumeRandomAcceptedNotification(Guid.NewGuid(), TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.AcceptRandoms);
	}

	[Fact]
	public async Task ProgressMissions_RandomPerfumeAddedNotificationHandler_UpdatesProgress() {
		await PrepareMissionData();
		using var scope = GetTestScope();
		var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
		var handler = new ProgressMissions.RandomPerfumeAddedNotificationHandler(updateHandler);
		var notification = new RandomPerfumeAddedNotification(Guid.NewGuid(), TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.GetRandoms);
	}

	void AssertProgress(MissionType type) {
		Assert.NotNull(HubMessages);
		Assert.NotEmpty(HubMessages);
		foreach (var m in HubMessages) {
			foreach (var s in m.HubSentArgs) {
				var mission = s as UserMissionDto;
				if (mission.MissionType == type) {
					Assert.NotNull(mission);
					Assert.True(mission.Progress > 0);
					HubMessages.Clear();
					return;
				}
			}
		}
		throw new Exception("Correct mission type not found in update args");
	}
}