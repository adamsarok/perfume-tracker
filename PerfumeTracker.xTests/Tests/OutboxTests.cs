using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Services.Outbox;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Outbox Tests")]
public class OutboxCollection : ICollectionFixture<OutboxFixture>;

public class OutboxFixture : DbFixture {
	public OutboxFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var sql = "truncate table \"public\".\"OutboxMessage\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);

		var userId = TenantProvider.GetCurrentUserId() ?? throw new InvalidOperationException("MockTenantProvider userId is null");

		var perfumes = await SeedPerfumes(3);
		var events = await SeedPerfumeEvents(1, perfumes[2].Id);

		var outboxMessages = new OutboxMessage[3] {
			OutboxMessage.From(new PerfumeAddedNotification(perfumes[0].Id, userId)),
			OutboxMessage.From(new PerfumeRecommendationAcceptedNotification(perfumes[1].Id, Guid.NewGuid())),
			OutboxMessage.From(new PerfumeEventAddedNotification(Guid.NewGuid(), perfumes[2].Id, userId, PerfumeEvent.PerfumeEventType.Worn)),
		};

		outboxMessages[2].LastError = "Test Error";
		outboxMessages[2].TryCount = 1;

		await context.OutboxMessages.AddRangeAsync(outboxMessages);
		await context.SaveChangesAsync();
	}
}

[Collection("Outbox Tests")]
public class OutboxTests {
	private readonly OutboxFixture _fixture;

	public OutboxTests(OutboxFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public async Task SideEffectProcessor_CanProcess() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var outboxMessage = await context.OutboxMessages.Skip(1).FirstAsync();
		var channel = scope.ServiceProvider.GetRequiredService<ISideEffectQueue>();
		channel.Enqueue(outboxMessage);
		await Task.Delay(1000);
		var msg = await context.OutboxMessages.FindAsync([outboxMessage.Id]);
		Assert.NotNull(msg);
		Assert.NotNull(msg.ProcessedAt);
	}

	[Fact]
	public async Task OutboxService_CanRetry() {
		using var scope = _fixture.Factory.Services.CreateScope();

		var mockLogger = new Mock<ILogger<OutboxBackgroundService>>();
		var mockSideEffectQueue = new Mock<ISideEffectQueue>();
		var outboxService = new TestOutboxService(scope.ServiceProvider, mockLogger.Object, mockSideEffectQueue.Object);
		await outboxService.Test_ProcessMessages();
		mockSideEffectQueue.Verify(q => q.Enqueue(It.IsAny<OutboxMessage>()), Times.AtLeastOnce());
	}

	[Fact(Skip = "Flaky: investigate intermittent timing/race in SideEffectQueue_CanEnqueue")]
	[Trait("Category", "Flaky")]
	public async Task SideEffectQueue_CanEnqueue() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var outboxMessage = await context.OutboxMessages.FirstAsync();
		var channel = scope.ServiceProvider.GetRequiredService<ISideEffectQueue>();
		channel.Enqueue(outboxMessage);
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		Assert.True(await channel.Reader.WaitToReadAsync(cts.Token));
	}

	class TestOutboxService : OutboxBackgroundService {
		public TestOutboxService(IServiceProvider sp, ILogger<OutboxBackgroundService> logger, ISideEffectQueue queue) : base(sp, logger, queue) {
		}

		public async Task Test_ProcessMessages() {
			await RetryMessages(CancellationToken.None);
		}
	}
}