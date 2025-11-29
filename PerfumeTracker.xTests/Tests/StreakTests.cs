using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.Streaks;
using static PerfumeTracker.Server.Services.Streaks.ProgressStreaks;
using Microsoft.AspNetCore.SignalR;
using PerfumeTracker.Server.Services.Common;
using PerfumeTracker.Server.Services.Streaks;
using PerfumeTracker.Server.Config;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Streak Tests")]
public class StreakCollection : ICollectionFixture<StreakFixture>;

public class StreakFixture : DbFixture {
	public StreakFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"public\".\"UserStreak\" CASCADE;");
	}
}

[Collection("Streak Tests")]
public class StreakTests {
	private readonly StreakFixture _fixture;

	public StreakTests(StreakFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public void GetXPMultiplier_ReturnsValidXP() {
		decimal last = 0;
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		
		var xpService = new XPService(context);
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
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		
		var queryHandler = new GetActiveStreaksHandler(context);
		var result = await queryHandler.Handle(new GetActiveStreaksQuery(), CancellationToken.None);
		Assert.NotNull(result);
	}

	[Fact]
	public async Task ProgressStreaks_PerfumeEventNotificationHandler_UpdatesProgress() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		
		var mockLogger = new Mock<ILogger<UpdateStreakProgressHandler>>();
		var userConfig = scope.ServiceProvider.GetRequiredService<UserConfiguration>();
		var updateHandler = new UpdateStreakProgressHandler(context, _fixture.MockStreakProgressHubContext.Object, 
			mockLogger.Object, userConfig);
		var handler = new StreakEventNotificationHandler(updateHandler);
		var notification = new PerfumeEventAddedNotification(Guid.NewGuid(), Guid.NewGuid(), _fixture.TenantProvider.MockTenantId ?? throw new TenantNotSetException(), PerfumeEvent.PerfumeEventType.Worn);
		await handler.Handle(notification, CancellationToken.None);
		AssertProgress();
	}

	[Theory]
	[InlineData("2024-07-10T00:00:00Z", "2024-07-10T23:59:59Z", 0, UpdateStreakProgressHandler.StreakStatus.NoChange)]
	[InlineData("2024-07-10T00:00:00Z", "2024-07-11T23:59:00Z", 0, UpdateStreakProgressHandler.StreakStatus.Progress)]
	[InlineData("2024-07-10T00:00:00Z", "2024-07-13T00:00:00Z", 0, UpdateStreakProgressHandler.StreakStatus.Ended)]
	[InlineData("2024-07-10T23:00:00Z", "2024-07-11T01:00:00Z", 2, UpdateStreakProgressHandler.StreakStatus.NoChange)]
	[InlineData("2024-07-10T23:00:00Z", "2024-07-12T01:00:00Z", 2, UpdateStreakProgressHandler.StreakStatus.Progress)]
	[InlineData("2024-07-10T23:00:00Z", "2024-07-10T21:00:00Z", -2, UpdateStreakProgressHandler.StreakStatus.NoChange)]
	[InlineData("2024-07-10T00:00:00Z", "2024-07-11T00:00:00Z", 0, UpdateStreakProgressHandler.StreakStatus.Progress)]
	public void GetStreakStatus_ReturnsExpectedStatus(string lastProgress, string now, int utcOffset, UpdateStreakProgressHandler.StreakStatus expected) {
		var lastProgressDate = DateTime.Parse(lastProgress, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
		var nowDate = DateTime.Parse(now, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		
		var mockLogger = new Mock<ILogger<UpdateStreakProgressHandler>>();
		var userConfig = scope.ServiceProvider.GetRequiredService<UserConfiguration>();
		var updateHandler = new UpdateStreakProgressHandler(context, _fixture.MockStreakProgressHubContext.Object,
			mockLogger.Object, userConfig);
		var result = updateHandler.GetStreakStatus(lastProgressDate, nowDate, utcOffset);

		Assert.Equal(expected, result);
	}

	void AssertProgress() {
		Assert.NotNull(_fixture.HubMessages);
		Assert.NotEmpty(_fixture.HubMessages);
		var hubSentArg = _fixture.HubMessages[0].HubSentArgs[0];
		var streakDto = hubSentArg as UserStreakDto;
		Assert.NotNull(streakDto);
		Assert.True(streakDto.Progress > 0);
		_fixture.HubMessages.Clear();
	}
}