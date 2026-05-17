using System.Diagnostics;
using System.Diagnostics.Metrics;
using OpenAI.Chat;

namespace PerfumeTracker.Server.Startup;

public static class Diagnostics {
	public const string ActivitySourceName = "PerfumeTracker.Server";
	public const string MeterName = "PerfumeTracker.Server";
	public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
	public static readonly Meter Meter = new(MeterName);
	public static readonly Counter<long> MissionsCompletedCounter = Meter.CreateCounter<long>(
		"missions.completed",
		unit: "{mission}",
		description: "Number of missions completed.");
	public static readonly Counter<long> RecommendationsAcceptedCounter = Meter.CreateCounter<long>(
		"recommendations.accepted",
		unit: "{recommendation}",
		description: "Number of perfume recommendations accepted.");
	public static readonly Counter<long> OpenAiChatTokensCounter = Meter.CreateCounter<long>(
		"openai.chat.tokens",
		unit: "{token}",
		description: "Number of tokens used by OpenAI chat completions.");

	public static void RecordChatTokenUsage(ChatCompletion completion, string operation) {
		var usage = completion.Usage;
		if (usage == null) return;

		OpenAiChatTokensCounter.Add(
			usage.InputTokenCount,
			new KeyValuePair<string, object?>("operation", operation),
			new KeyValuePair<string, object?>("token.type", "input"));
		OpenAiChatTokensCounter.Add(
			usage.OutputTokenCount,
			new KeyValuePair<string, object?>("operation", operation),
			new KeyValuePair<string, object?>("token.type", "output"));
		OpenAiChatTokensCounter.Add(
			usage.TotalTokenCount,
			new KeyValuePair<string, object?>("operation", operation),
			new KeyValuePair<string, object?>("token.type", "total"));
	}
}
