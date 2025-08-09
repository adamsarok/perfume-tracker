using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.Streaks;
using static PerfumeTracker.Server.Services.Streaks.ProgressStreaks;
using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Services.Common;
using PerfumeTracker.Server.Services.Streaks;

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
	public void GetXPMultiplier_ReturnsValidXP() {
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

    [Theory]
    [InlineData("2024-07-10T00:00:00Z", "2024-07-10T23:59:59Z", 0, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus.NoChange)] // same day
    [InlineData("2024-07-10T00:00:00Z", "2024-07-11T23:59:00Z", 0, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus.Progress)] // next day, whole day
    [InlineData("2024-07-10T00:00:00Z", "2024-07-12T00:00:00Z", 0, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus.Ended)]	   // more than streakProtectionDays
    [InlineData("2024-07-10T23:00:00Z", "2024-07-11T01:00:00Z", 2, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus.NoChange)] // same day with offset
    [InlineData("2024-07-10T23:00:00Z", "2024-07-12T01:00:00Z", 2, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus.Progress)] // progress with offset
    [InlineData("2024-07-10T23:00:00Z", "2024-07-10T21:00:00Z", -2, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus.NoChange)] // negative offset, same day
	[InlineData("2024-07-10T00:00:00Z", "2024-07-11T00:00:00Z", 0, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus.Progress)] // exactly 1-day boundary
	public void GetStreakStatus_ReturnsExpectedStatus(string lastProgress, string now, int utcOffset, ProgressStreaks.UpdateStreakProgressHandler.StreakStatus expected)
    {
        var lastProgressDate = DateTime.Parse(lastProgress, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
        var nowDate = DateTime.Parse(now, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);

        var result = UpdateStreakProgressHandler.GetStreakStatus(lastProgressDate, nowDate, utcOffset);  

		Assert.Equal(expected, result);	
	}

	void AssertProgress() {
		Assert.NotNull(HubMessages);
		Assert.NotEmpty(HubMessages);
		var hubSentArg = HubMessages[0].HubSentArgs[0];
		var streakDto = hubSentArg as UserStreakDto;
		Assert.NotNull(streakDto);
		Assert.True(streakDto.Progress > 0);
		HubMessages.Clear();
	}
}