using System.Text.Json.Nodes;
using Polly;
using Polly.Retry;

namespace PerfumeTracker.Server.Features.WeeklyDigests;

public static class DigestGenerationRetry {
	public static bool IsTransient(Exception? error) => error is DigestApiException { IsTransient: true }
		or HttpRequestException or TimeoutException or TaskCanceledException;

	public static ResiliencePipeline<JsonObject> Create(WeeklyDigestOptions options, Func<bool> canRetry,
		ILogger logger, TimeProvider? timeProvider = null) {
		var builder = new ResiliencePipelineBuilder<JsonObject>();
		if (timeProvider != null) builder.TimeProvider = timeProvider;
		if (options.MaxGenerationRetries == 0) return builder.Build();
		return builder.AddRetry(new RetryStrategyOptions<JsonObject> {
			MaxRetryAttempts = options.MaxGenerationRetries,
			Delay = TimeSpan.FromSeconds(options.RetryBaseDelaySeconds),
			MaxDelay = TimeSpan.FromSeconds(options.RetryMaxDelaySeconds),
			BackoffType = DelayBackoffType.Exponential,
			UseJitter = true,
			ShouldHandle = args => ValueTask.FromResult(!args.Context.CancellationToken.IsCancellationRequested
				&& IsTransient(args.Outcome.Exception) && canRetry()),
			// Returning null delegates to Polly's exponential jitter calculation. A server delay is
			// deliberately not shortened by the fallback cap, even when Retry-After exceeds it.
			DelayGenerator = args => ValueTask.FromResult(args.Outcome.Exception is DigestApiException api ? api.RetryAfter : null),
			OnRetry = args => {
				logger.LogWarning(args.Outcome.Exception, "Retrying weekly digest generation. Retry {Retry}/{MaxRetries}, delay {Delay}, server Retry-After {RetryAfter}",
					args.AttemptNumber + 1, options.MaxGenerationRetries, args.RetryDelay, (args.Outcome.Exception as DigestApiException)?.RetryAfter);
				return default;
			}
		}).Build();
	}
}
