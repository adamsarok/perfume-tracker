#pragma warning disable OPENAI001 // Responses API is marked experimental in OpenAI 2.13.0.
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Responses;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.Evals.Recording;

public sealed class MissingRecordingException(string fingerprint, string caseId, int callIndex)
	: Exception($"No recorded response for fingerprint {fingerprint}, case '{caseId}', call {callIndex}. Re-run in --mode record.");

/// <summary>
/// The <see cref="IResponsesClient"/> the agent sees during an eval run. It boots the real
/// model call in real/record mode, replays from disk in replay mode, and counts calls/tokens
/// in every mode. Replay also reconstructs reasoning items, because the SDK's model writer
/// round-trips <see cref="ResponseResult"/> including encrypted reasoning content.
/// </summary>
public sealed class EvalResponsesClient(EvalRunOptions runOptions, EvalRecordingStore store) : IResponsesClient {
	private readonly object gate = new();
	private RecordingSession? session;

	public string Fingerprint { get; set; } = "unfingerprinted";

	/// <summary>Set by the host after startup so real/record runs can reach the configured client.</summary>
	public IServiceProvider? InnerServices { get; set; }

	public void BeginCase(RecordingSession recordingSession) => session = recordingSession;
	public void EndCase() => session = null;

	public async Task<ResponseResult> CreateResponseAsync(CreateResponseOptions options, CancellationToken cancellationToken) {
		var current = session ?? throw new InvalidOperationException("An eval case must be started before the model is called.");
		int callIndex;
		lock (gate) callIndex = current.NextCallIndex();

		// Title generation is the only call built without tools.
		var isTitleCall = options.Tools.Count == 0;

		if (runOptions.Mode == EvalMode.Replay) {
			var recorded = await store.TryLoadAsync(Fingerprint, current.CaseId, callIndex, cancellationToken);
			if (recorded == null) {
				current.MissingRecording = true;
				current.MissingRecordingDetail = $"case '{current.CaseId}' call {callIndex}";
				throw new MissingRecordingException(Fingerprint, current.CaseId, callIndex);
			}
			current.Observe(recorded, isTitleCall);
			return recorded;
		}

		var inner = InnerServices?.GetService<ResponsesClientAdapter>()
			?? throw new InvalidOperationException("No model client is configured. Set OpenAI:ApiKey or pass --api-key.");
		var response = await inner.CreateResponseAsync(options, cancellationToken);
		current.Observe(response, isTitleCall);
		if (runOptions.Mode == EvalMode.Record) {
			await store.SaveAsync(Fingerprint, current.CaseId, callIndex, response, cancellationToken);
		}
		return response;
	}
}
