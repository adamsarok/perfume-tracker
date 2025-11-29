using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Services.Outbox;
using PerfumeTracker.xTests.Fixture;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Outbox Tests")]
public class OutboxCollection : ICollectionFixture<OutboxFixture>;

public class OutboxFixture : DbFixture {
	public OutboxFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		var sql = "truncate table \"public\".\"OutboxMessage\" cascade; ";
		await context.Database.ExecuteSqlRawAsync(sql);
		
		var userId = TenantProvider.GetCurrentUserId() ?? throw new InvalidOperationException("MockTenantProvider userId is null");
		var outboxMessages = OutboxMessageFaker.Clone()
			.RuleFor(om => om.UserId, userId)
			.Generate(3);
		
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
		
		var mockLogger = new Mock<ILogger<OutboxRetryService>>();
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

	class TestOutboxService : OutboxRetryService {
		public TestOutboxService(IServiceProvider sp, ILogger<OutboxRetryService> logger, ISideEffectQueue queue) : base(sp, logger, queue) {
		}

		public async Task Test_ProcessMessages() {
			await RetryMessages(CancellationToken.None);
		}
	}
}