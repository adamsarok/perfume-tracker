using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Services.Outbox;
using System.Threading.Tasks;

namespace PerfumeTracker.xTests;
public class OutboxTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public OutboxTests(WebApplicationFactory<Program> factory) : base(factory) { }

	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task<List<OutboxMessage>> PrepareData() {
		await semaphore.WaitAsync();
		try {
			using var scope = GetTestScope();
			var sql = "truncate table \"public\".\"OutboxMessage\" cascade; ";
			await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync(sql);
			var userId = TenantProvider.GetCurrentUserId() ?? throw new InvalidOperationException("MockTenantProvider userId is null");
			var outboxSeed = new List<OutboxMessage>() {
				OutboxMessage.From(new PerfumeEventAddedNotification(Guid.NewGuid(), Guid.NewGuid(), userId)),
				OutboxMessage.From(new PerfumeAddedNotification(Guid.NewGuid(), userId)),
				OutboxMessage.From(new PerfumeAddedNotification(Guid.NewGuid(), userId)),
			};
			outboxSeed[2].LastError = "Test Error";
			outboxSeed[2].TryCount = 1;
			scope.PerfumeTrackerContext.OutboxMessages.AddRange(outboxSeed);
			await scope.PerfumeTrackerContext.SaveChangesAsync();
			return outboxSeed;
		} finally {
			semaphore.Release();
		}
	}

	[Fact]
	public async Task SideEffectProcessor_CanProcess() {
		var outboxSeed = await PrepareData();
		using var scope = GetTestScope();
		var channel = scope.ServiceScope.ServiceProvider.GetRequiredService<ISideEffectQueue>();
		channel.Enqueue(outboxSeed[1]);
		await Task.Delay(1000);
		var msg = await scope.PerfumeTrackerContext.OutboxMessages.FindAsync(outboxSeed[1].Id);
		Assert.NotNull(msg);
		Assert.NotNull(msg.ProcessedAt);
	}

	[Fact]
	public async Task OutboxService_CanRetry() {
		await PrepareData();
		using var scope = GetTestScope();
		var mockLogger = new Mock<ILogger<OutboxService>>();
		var mockSideEffectQueue = new Mock<ISideEffectQueue>();
		var outboxService = new TestOutboxService(scope.ServiceScope.ServiceProvider, mockLogger.Object, mockSideEffectQueue.Object);
		await outboxService.Test_ProcessMessages();
		mockSideEffectQueue.Verify(q => q.Enqueue(It.IsAny<OutboxMessage>()), Times.AtLeastOnce());
	}

	class TestOutboxService : OutboxService {
		public TestOutboxService(IServiceProvider sp, ILogger<OutboxService> logger, ISideEffectQueue queue) : base(sp, logger, queue) {
		}

		public async Task Test_ProcessMessages() {
			await base.ProcessMessages(CancellationToken.None);
		}
	}
}