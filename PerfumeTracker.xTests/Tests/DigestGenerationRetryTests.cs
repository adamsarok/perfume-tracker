using System.Collections.Concurrent;
using System.Net;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.xTests.Tests;

public class DigestGenerationRetryTests {
	[Theory]
	[InlineData("12", 12)]
	[InlineData("Mon, 12 Oct 2026 12:00:20 GMT", 20)]
	[InlineData("Mon, 12 Oct 2026 11:59:00 GMT", 0)]
	[InlineData("invalid", null)]
	[InlineData("-5", null)]
	public async Task Transport_PropagatesRetryAfter(string value, int? seconds) {
		using var http = new HttpClient(new HeaderHandler(value)) { BaseAddress = new Uri("https://example.invalid/") };
		var model = new DigestModel(http, Options.Create(new OpenAIOptions { ApiKey = "test" }), new FixedClock());
		var error = await Assert.ThrowsAsync<DigestApiException>(() => model.Generate(new(), TestContext.Current.CancellationToken));
		Assert.Equal(520, error.StatusCode);
		Assert.Equal(seconds.HasValue ? TimeSpan.FromSeconds(seconds.Value) : null, error.RetryAfter);
	}

	[Theory]
	[InlineData(408)]
	[InlineData(429)]
	[InlineData(503)]
	[InlineData(520)]
	public async Task Policy_HonorsServerDelayBeyondFallbackCap(int status) {
		var clock = new ImmediateClock();
		var pipeline = DigestGenerationRetry.Create(new(), () => true, NullLogger.Instance, clock);
		var calls = 0;
		await pipeline.ExecuteAsync(_ => {
			if (++calls == 1) throw new DigestApiException(status, TimeSpan.FromMinutes(2));
			return ValueTask.FromResult(new JsonObject());
		}, TestContext.Current.CancellationToken);
		Assert.Equal(2, calls);
		Assert.Equal(TimeSpan.FromMinutes(2), Assert.Single(clock.Delays));
	}

	[Fact]
	public async Task Policy_FallsBackToBoundedJitter_AndStopsAfterThreeRetries() {
		var clock = new ImmediateClock();
		var pipeline = DigestGenerationRetry.Create(new(), () => true, NullLogger.Instance, clock);
		var calls = 0;
		await Assert.ThrowsAsync<HttpRequestException>(async () => await pipeline.ExecuteAsync(_ => {
			calls++; return ValueTask.FromException<JsonObject>(new HttpRequestException("Temporary network fault"));
		}, TestContext.Current.CancellationToken));
		Assert.Equal(4, calls);
		Assert.Equal(3, clock.Delays.Count);
		Assert.All(clock.Delays, delay => Assert.InRange(delay, TimeSpan.Zero, TimeSpan.FromSeconds(30)));
		Assert.Contains(clock.Delays, delay => delay > TimeSpan.Zero);
	}

	[Theory]
	[InlineData(400)]
	[InlineData(401)]
	[InlineData(403)]
	public async Task Policy_DoesNotRetryPermanentErrors(int status) {
		var clock = new ImmediateClock();
		var pipeline = DigestGenerationRetry.Create(new(), () => true, NullLogger.Instance, clock);
		var calls = 0;
		await Assert.ThrowsAsync<DigestApiException>(async () => await pipeline.ExecuteAsync(_ => {
			calls++; return ValueTask.FromException<JsonObject>(new DigestApiException(status));
		}, TestContext.Current.CancellationToken));
		Assert.Equal(1, calls);
		Assert.Empty(clock.Delays);
	}

	[Fact]
	public async Task Policy_CancellationInterruptsRetryAfterWait() {
		using var cancellation = new CancellationTokenSource();
		var pipeline = DigestGenerationRetry.Create(new(), () => true, NullLogger.Instance);
		var calls = 0;
		await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await pipeline.ExecuteAsync(_ => {
			calls++;
			cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
			return ValueTask.FromException<JsonObject>(new DigestApiException(520, TimeSpan.FromHours(1)));
		}, cancellation.Token).AsTask().WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
		Assert.Equal(1, calls);
	}

	private sealed class HeaderHandler(string header) : HttpMessageHandler {
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) {
			var response = new HttpResponseMessage((HttpStatusCode)520);
			response.Headers.TryAddWithoutValidation("Retry-After", header);
			return Task.FromResult(response);
		}
	}
	private sealed class FixedClock : TimeProvider {
		public override DateTimeOffset GetUtcNow() => new(2026, 10, 12, 12, 0, 0, TimeSpan.Zero);
	}
	private sealed class ImmediateClock : TimeProvider {
		public ConcurrentQueue<TimeSpan> Delays { get; } = new();
		public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period) {
			Delays.Enqueue(dueTime);
			return new ImmediateTimer(callback, state);
		}
		private sealed class ImmediateTimer : ITimer {
			private int disposed;
			public ImmediateTimer(TimerCallback callback, object? state) => ThreadPool.QueueUserWorkItem(_ => { if (Volatile.Read(ref disposed) == 0) callback(state); });
			public bool Change(TimeSpan dueTime, TimeSpan period) => true;
			public void Dispose() => Interlocked.Exchange(ref disposed, 1);
			public ValueTask DisposeAsync() { Dispose(); return default; }
		}
	}
}
