namespace PerfumeTracker.Evals.Cases;

public sealed class EvalDataset {
	public int Version { get; set; } = 1;
	public List<EvalCase> Cases { get; set; } = [];
}

/// <summary>
/// A single chat agent eval case. <see cref="ExpectedTools"/>/<see cref="ForbiddenTools"/> are
/// checked deterministically; <see cref="Rubric"/> is handed to the LLM judge.
/// </summary>
public sealed class EvalCase {
	public string Id { get; set; } = string.Empty;
	public string Category { get; set; } = "general";
	public string Message { get; set; } = string.Empty;

	/// <summary>Tools that must appear in the trajectory.</summary>
	public List<string> ExpectedTools { get; set; } = [];

	/// <summary>Tools that must not appear in the trajectory.</summary>
	public List<string> ForbiddenTools { get; set; } = [];

	/// <summary>Owned perfume names ("House - PerfumeName") the answer must link to.</summary>
	public List<string> ExpectedOwnedLinks { get; set; } = [];

	/// <summary>Owned perfume names that must not be linked in the answer.</summary>
	public List<string> ForbiddenOwnedLinks { get; set; } = [];

	/// <summary>Each assertion must match at least one call, allowing additional arguments.</summary>
	public List<ExpectedToolArguments> RequiredToolArguments { get; set; } = [];

	/// <summary>True when the case needs real embeddings / chat calls and cannot be replayed.</summary>
	public bool RequiresApi { get; set; }

	/// <summary>Upper bound on tool calls for this case, when relevant.</summary>
	public int? MaxToolCalls { get; set; }

	public bool Judge { get; set; } = true;

	/// <summary>What a good answer must contain, in plain language, for the LLM judge.</summary>
	public string? Rubric { get; set; }
}

public sealed class ExpectedToolArguments {
	public string Tool { get; set; } = string.Empty;
	public Dictionary<string, System.Text.Json.JsonElement> Arguments { get; set; } = [];
}
