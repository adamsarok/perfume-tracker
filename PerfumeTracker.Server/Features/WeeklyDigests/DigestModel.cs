using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace PerfumeTracker.Server.Features.WeeklyDigests;

public interface IDigestModel {
	Task<JsonObject> Generate(JsonObject request, CancellationToken ct);
}

// A dedicated client avoids the SDK's automatic generation retries: each billed request
// must be checkpointed by the runner before it is sent.
public class DigestModel(HttpClient http, IOptions<OpenAIOptions> options, TimeProvider? timeProvider = null) : IDigestModel {
	private async Task<JsonObject> Post(string path, JsonObject request, CancellationToken ct) {
		using var message = new HttpRequestMessage(HttpMethod.Post, path) {
			Content = JsonContent.Create(request)
		};
		message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
		using var response = await http.SendAsync(message, ct);
		if (!response.IsSuccessStatusCode)
			throw new DigestApiException((int)response.StatusCode, ReadRetryAfter(response, (timeProvider ?? TimeProvider.System).GetUtcNow()));
		return (await response.Content.ReadFromJsonAsync<JsonObject>(ct))
			?? throw new InvalidDataException("OpenAI returned an empty response.");
	}
	public Task<JsonObject> Generate(JsonObject request, CancellationToken ct) => Post("responses", request, ct);

	public static TimeSpan? ReadRetryAfter(HttpResponseMessage response, DateTimeOffset now) {
		if (!response.Headers.TryGetValues("Retry-After", out var values)
			|| !RetryConditionHeaderValue.TryParse(values.FirstOrDefault(), out var header)) return null;
		if (header.Delta is { } delta) return delta >= TimeSpan.Zero ? delta : null;
		if (header.Date is { } date) return date > now ? date - now : TimeSpan.Zero;
		return null;
	}
}

public class DigestApiException(int statusCode, TimeSpan? retryAfter = null) : Exception($"OpenAI request failed with HTTP {statusCode}.") {
	public int StatusCode { get; } = statusCode;
	public TimeSpan? RetryAfter { get; } = retryAfter;
	public bool IsTransient => statusCode is 408 or 429 or (>= 500 and <= 599);
}
public class DigestBudgetException(string message) : Exception(message);
