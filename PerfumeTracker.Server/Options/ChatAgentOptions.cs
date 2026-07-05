namespace PerfumeTracker.Server.Options;

public class ChatAgentOptions {
	public int MaxIterations { get; set; } = 4;
	public int MaxResultsPerToolCall { get; set; } = 10;
}
