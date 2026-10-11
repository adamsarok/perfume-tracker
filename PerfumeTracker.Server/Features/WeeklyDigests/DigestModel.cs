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
public class DigestModel(HttpClient http, IOptions<OpenAIOptions> options) : IDigestModel {
	private async Task<JsonObject> Post(string path, JsonObject request, CancellationToken ct) {
		using var message = new HttpRequestMessage(HttpMethod.Post, path) {
			Content = JsonContent.Create(request)
		};
		message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
		using var response = await http.SendAsync(message, ct);
		if (!response.IsSuccessStatusCode)
			throw new DigestApiException((int)response.StatusCode);
		return (await response.Content.ReadFromJsonAsync<JsonObject>(ct))
			?? throw new InvalidDataException("OpenAI returned an empty response.");
	}
	public Task<JsonObject> Generate(JsonObject request, CancellationToken ct) => Post("responses", request, ct);
}

public class DigestApiException(int statusCode) : Exception($"OpenAI request failed with HTTP {statusCode}.") {
	public bool IsTransient => statusCode is 408 or 429 || statusCode >= 500;
}
public class DigestBudgetException(string message) : Exception(message);
