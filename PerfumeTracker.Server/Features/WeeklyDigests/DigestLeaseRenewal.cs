namespace PerfumeTracker.Server.Features.WeeklyDigests;

/// <summary>Disposing the timer ends normal renewal without cancelling a waiting delay.</summary>
public sealed class DigestLeaseRenewal : IAsyncDisposable {
	private readonly PeriodicTimer timer;
	private readonly CancellationTokenRegistration shutdown;
	private readonly Task loop;
	private int stopped;

	public DigestLeaseRenewal(Guid digestId, Func<CancellationToken, Task<bool>> renew,
		CancellationTokenSource work, CancellationToken stoppingToken, ILogger logger, TimeSpan? interval = null) {
		timer = new PeriodicTimer(interval ?? TimeSpan.FromSeconds(30));
		shutdown = stoppingToken.Register(timer.Dispose);
		loop = Run();

		async Task Run() {
			try {
				while (await timer.WaitForNextTickAsync()) {
					if (Volatile.Read(ref stopped) != 0 || stoppingToken.IsCancellationRequested) return;
					var owned = await renew(stoppingToken);
					if (!owned) {
						if (Volatile.Read(ref stopped) == 0 && !stoppingToken.IsCancellationRequested) {
							logger.LogWarning("Weekly digest lease ownership lost. Digest {DigestId}; cancelling generation", digestId);
							await work.CancelAsync();
						}
						return;
					}
				}
			} catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
				// Host shutdown can cancel an in-flight database command; timer disposal itself does not throw.
			} catch (Exception ex) {
				logger.LogError(ex, "Weekly digest lease renewal failed. Digest {DigestId}; cancelling generation", digestId);
				await work.CancelAsync();
			}
		}
	}

	public async Task StopAsync() {
		Interlocked.Exchange(ref stopped, 1);
		timer.Dispose();
		await loop;
	}

	public async ValueTask DisposeAsync() {
		await StopAsync();
		shutdown.Dispose();
	}
}
