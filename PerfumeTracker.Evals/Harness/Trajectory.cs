namespace PerfumeTracker.Evals.Harness;

public sealed record ConversationSnapshot(List<ChatMessage> Messages, string? Title, List<Guid> DiscussedPerfumeIds);

public sealed record ToolCallTrace(string Id, string Name, string Arguments, string? Result);

public sealed class CaseTrajectory {
	public required string CaseId { get; init; }
	public required string UserMessage { get; init; }
	public required string FinalAnswer { get; init; }
	public string? Title { get; init; }
	public Guid ConversationId { get; init; }
	public int ChatModelCalls { get; init; }
	public int TitleModelCalls { get; init; }
	public int InputTokens { get; init; }
	public int OutputTokens { get; init; }
	public long DurationMs { get; init; }
	public string PromptFingerprint { get; init; } = string.Empty;
	public IReadOnlyList<ToolCallTrace> ToolCalls { get; init; } = [];
	public IReadOnlyList<Guid> DiscussedPerfumeIds { get; init; } = [];
	public string? Error { get; init; }

	public IReadOnlyList<string> CalledTools => ToolCalls.Select(call => call.Name).Distinct(StringComparer.Ordinal).ToList();
}
