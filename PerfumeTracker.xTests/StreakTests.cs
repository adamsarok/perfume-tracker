using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.PerfumeEvents;
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

	[Fact]
	public async Task GetXPMultiplier_ReturnsValidXP() {
		decimal last = 0;
		using var scope = GetTestScope();
		var xpService = new XPService(scope.PerfumeTrackerContext);
		for (int i = 0; i < 500; i++) {
			decimal multiplier = xpService.GetXPMultiplier(i);
			Assert.True(multiplier > last || multiplier == XPService.MaxMultiplier);
			Assert.True(multiplier <= XPService.MaxMultiplier);
			last = multiplier;
			Console.WriteLine(multiplier);
		}
	}

	[Fact]
	public async Task GetActiveStreaksHandler_ReturnsActiveStreaks() {
		await PrepareDb();
		using var scope = GetTestScope();
		var queryHandler = new GetActiveStreaksHandler(scope.PerfumeTrackerContext);
		var result = await queryHandler.Handle(new GetActiveStreaksQuery(), CancellationToken.None);
		Assert.NotNull(result);
	}

	[Fact]
	public async Task ProgressStreaks_PerfumeEventNotificationHandler_UpdatesProgress() {
		await PrepareDb();
		using var scope = GetTestScope();
		var mockLogger = new Mock<ILogger<UpdateStreakProgressHandler>>();
		var updateHandler = new UpdateStreakProgressHandler(scope.PerfumeTrackerContext, MockStreakProgressHubContext.Object, mockLogger.Object);
		var handler = new StreakEventNotificationHandler(updateHandler);
		var notification = new PerfumeEventAddedNotification(Guid.NewGuid(), Guid.NewGuid(), TenantProvider.MockTenantId ?? throw new TenantNotSetException());
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress();
	}


	void AssertProgress() {
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
		throw new Exception("Streak not found in update args");
	}
}