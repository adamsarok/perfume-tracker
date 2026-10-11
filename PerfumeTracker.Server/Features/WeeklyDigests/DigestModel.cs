using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace PerfumeTracker.Server.Features.WeeklyDigests;

public interface IDigestModel {
	Task<int> CountInput(JsonObject request, CancellationToken ct);
	Task<JsonObject> Generate(JsonObject request, CancellationToken ct);
}

// A dedicated client avoids the SDK's automatic generation retries: each billed request
// must be reserved and checkpointed by the runner before it is sent.
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
	public async Task<int> CountInput(JsonObject request, CancellationToken ct) {
		var countRequest = new JsonObject();
		foreach (var key in new[] { "model", "input", "tools", "tool_choice", "text", "reasoning", "instructions" })
			if (request[key] != null) countRequest[key] = request[key]!.DeepClone();
		var result = await Post("responses/input_tokens", countRequest, ct);
		var count = result["input_tokens"]?.GetValue<int>() ?? throw new InvalidDataException("Missing input token count.");
		return count >= 0 ? count : throw new InvalidDataException("Invalid input token count.");
	}
	public Task<JsonObject> Generate(JsonObject request, CancellationToken ct) => Post("responses", request, ct);
}

public class DigestApiException(int statusCode) : Exception($"OpenAI request failed with HTTP {statusCode}.") {
	public bool IsTransient => statusCode is 408 or 429 || statusCode >= 500;
}
public class DigestBudgetException(string message) : Exception(message);
