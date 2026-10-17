using OpenAI.Responses;

namespace PerfumeTracker.Evals.Recording;

/// <summary>Per-case bookkeeping: what the model was asked for and what it cost.</summary>
public sealed class RecordingSession(string caseId) {
	public string CaseId { get; } = caseId;
	public int ChatModelCalls { get; private set; }
	public int TitleModelCalls { get; private set; }
	public int InputTokens { get; private set; }
	public int OutputTokens { get; private set; }
	public bool MissingRecording { get; set; }
	public string? MissingRecordingDetail { get; set; }

	private int nextCallIndex;

	public int NextCallIndex() => nextCallIndex++;

	public void Observe(ResponseResult response, bool isTitleCall) {
		if (isTitleCall) TitleModelCalls++;
		else ChatModelCalls++;
		InputTokens += response.Usage?.InputTokenCount ?? 0;
		OutputTokens += response.Usage?.OutputTokenCount ?? 0;
	}
}
