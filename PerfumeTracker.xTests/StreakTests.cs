using Microsoft.AspNetCore.Mvc.Testing;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.PerfumeRandoms;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Features.Streaks;
using static PerfumeTracker.Server.Features.Streaks.ProgressStreaks;

namespace PerfumeTracker.xTests;

public class StreakTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public StreakTests(WebApplicationFactory<Program> factory) : base(factory) { }

	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);

	private async Task PrepareDb() {
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = GetTestScope();
				await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"public\".\"UserStreak\" CASCADE;");
				dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	//[Fact]
	//public async Task ProgressStreaks_CreatesStreak() {
	//	await PrepareDb();
	//	using var scope = GetTestScope();
	//	var handler = new UpdateStreakProgressHandler(scope.PerfumeTrackerContext);
	//	await handler.Handle(new GenerateMissionCommand(), CancellationToken.None);
	//	var userMissions = await scope.PerfumeTrackerContext.UserMissions.ToListAsync();
	//	Assert.NotEmpty(userMissions);
	//}

	//[Fact] //TODO
	//public async Task GetActiveMissionsHandler_ReturnsActiveMissions() {
	//	await PrepareMissionData();
	//	using var scope = GetTestScope();
	//	var handlerc = new GenerateMissions(scope.PerfumeTrackerContext);
	//	await handlerc.Handle(new GenerateMissionCommand(), CancellationToken.None);
	//	var handler = new GetActiveMissionsHandler(scope.PerfumeTrackerContext);
	//	var result = await handler.Handle(new GetActiveMissionsQuery(), CancellationToken.None);
	//	Assert.NotNull(result);
	//	Assert.NotEmpty(result);
	//}

	[Fact]
	public async Task ProgressStreaks_PerfumeEventNotificationHandler_UpdatesProgress() {
		await PrepareDb();
		using var scope = GetTestScope();
		var updateHandler = new ProgressStreaks.UpdateStreakProgressHandler(scope.PerfumeTrackerContext, MockStreakProgressHubContext.Object);
		var handler = new ProgressStreaks.StreakEventNotificationHandler(updateHandler);
		var notification = new PerfumeEventAddedNotification(Guid.NewGuid(), Guid.NewGuid(), TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress(MissionType.WearPerfumes);
	}


	void AssertProgress(MissionType type) {
		Assert.NotNull(HubMessages);
		Assert.NotEmpty(HubMessages);
		foreach (var m in HubMessages) {
			foreach (var s in m.HubSentArgs) {
				var streakDto = s as UserStreakDto;
				Assert.NotNull(streakDto);
				Assert.True(streakDto.Progress > 0);
				HubMessages.Clear();
				return;
			}
		}
		throw new Exception("Correct mission type not found in update args");
	}
}