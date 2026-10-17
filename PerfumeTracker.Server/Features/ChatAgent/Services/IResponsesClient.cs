#pragma warning disable OPENAI001 // Responses API is marked experimental in OpenAI 2.13.0.
using OpenAI.Responses;

namespace PerfumeTracker.Server.Features.ChatAgent.Services;

/// <summary>
/// Seam over the OpenAI Responses API. The chat agent depends on this instead of the
/// concrete <see cref="ResponsesClient"/> so the agent loop can be driven by recordings,
/// fakes, or a different model (the eval harness does all three).
/// </summary>
public interface IResponsesClient {
	Task<ResponseResult> CreateResponseAsync(CreateResponseOptions options, CancellationToken cancellationToken);
}

public class ResponsesClientAdapter(ResponsesClient responsesClient) : IResponsesClient {
	public async Task<ResponseResult> CreateResponseAsync(CreateResponseOptions options, CancellationToken cancellationToken)
		=> (await responsesClient.CreateResponseAsync(options, cancellationToken)).Value;
}
