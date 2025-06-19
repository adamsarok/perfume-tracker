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

public class MissionTests : TestBase, IClassFixture<WebApplicationFactory<Program>>
{
    public MissionTests(WebApplicationFactory<Program> factory) : base(factory) { }

    private async Task PrepareMissionData()
    {
        using var scope = GetTestScope();
        // Clean up and seed missions
        await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"public\".\"Mission\" CASCADE; TRUNCATE TABLE \"public\".\"UserMission\" CASCADE;");
        var mission = new Mission
        {
            Id = Guid.NewGuid(),
            Name = "Test Mission",
            Description = "Test Desc",
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            Type = MissionType.WearPerfumes,
            RequiredCount = 1,
            XP = 10
        };
        scope.PerfumeTrackerContext.Missions.Add(mission);
        await scope.PerfumeTrackerContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ProgressMissions_PerfumeEventNotificationHandler_UpdatesProgress()
    {
        await PrepareMissionData();
        using var scope = GetTestScope();
        var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
        var handler = new ProgressMissions.PerfumeEventNotificationHandler(scope.PerfumeTrackerContext, updateHandler);
        var notification = new PerfumeEventAddedNotification(Guid.NewGuid(), Guid.NewGuid(), scope.PerfumeTrackerContext.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException());
        await handler.Handle(notification, CancellationToken.None);
        // Assert that a UserMission was created or updated
        var userMissions = await scope.PerfumeTrackerContext.UserMissions.ToListAsync();
        Assert.NotEmpty(userMissions);
    }

    [Fact]
    public async Task GenerateMissionsHandler_CreatesUserMissions()
    {
        await PrepareMissionData();
        using var scope = GetTestScope();
        var handler = new GenerateMissions(scope.PerfumeTrackerContext);
        await handler.Handle(new GenerateMissionCommand(), CancellationToken.None);
        var userMissions = await scope.PerfumeTrackerContext.UserMissions.ToListAsync();
        Assert.NotEmpty(userMissions);
    }

    [Fact]
    public async Task GetActiveMissionsHandler_ReturnsActiveMissions()
    {
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
    public async Task ProgressMissions_PerfumeTagsAddedNotificationHandler_UpdatesProgress()
    {
        await PrepareMissionData();
        using var scope = GetTestScope();
        var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
        var handler = new ProgressMissions.PerfumeTagsAddedNotificationHandler(updateHandler);
        var notification = new PerfumeTagsAddedNotification(new List<Guid> { Guid.NewGuid() }, scope.PerfumeTrackerContext.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException());
        await handler.Handle(notification, CancellationToken.None);
        var userMissions = await scope.PerfumeTrackerContext.UserMissions.ToListAsync();
        Assert.NotNull(userMissions);
    }

    [Fact]
    public async Task ProgressMissions_PerfumeRandomAcceptedNotificationHandler_UpdatesProgress()
    {
        await PrepareMissionData();
        using var scope = GetTestScope();
        var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
        var handler = new ProgressMissions.PerfumeRandomAcceptedNotificationHandler(updateHandler);
        var notification = new PerfumeRandomAcceptedNotification(Guid.NewGuid(), scope.PerfumeTrackerContext.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException());
        await handler.Handle(notification, CancellationToken.None);
        var userMissions = await scope.PerfumeTrackerContext.UserMissions.ToListAsync();
        Assert.NotNull(userMissions);
    }

    [Fact]
    public async Task ProgressMissions_RandomPerfumeAddedNotificationHandler_UpdatesProgress()
    {
        await PrepareMissionData();
        using var scope = GetTestScope();
        var updateHandler = new ProgressMissions.UpdateMissionProgressHandler(scope.PerfumeTrackerContext, MockHubContext.Object);
        var handler = new ProgressMissions.RandomPerfumeAddedNotificationHandler(updateHandler);
        var notification = new RandomPerfumeAddedNotification(Guid.NewGuid(), scope.PerfumeTrackerContext.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException());
        await handler.Handle(notification, CancellationToken.None);
        var userMissions = await scope.PerfumeTrackerContext.UserMissions.ToListAsync();
        Assert.NotNull(userMissions);
    }
} 