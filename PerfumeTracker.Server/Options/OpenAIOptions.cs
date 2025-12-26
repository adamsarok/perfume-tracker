namespace PerfumeTracker.Server.Options;

public class OpenAIOptions {
	public string ApiKey { get; set; } = string.Empty;
	public string EmbeddingsModel { get; set; } = "text-embedding-3-small";
	public string AssistantModel { get; set; } = "gpt-4o-mini";
}
