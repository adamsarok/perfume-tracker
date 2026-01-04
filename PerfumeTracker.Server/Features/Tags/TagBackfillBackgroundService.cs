using OpenAI.Chat;
using System.Text;

namespace PerfumeTracker.Server.Features.Tags;

public class TagBackfillBackgroundService(IServiceProvider sp, ChatClient chatClient, ILogger<TagBackfillBackgroundService> logger) : BackgroundService {
	private const int MAX_BACKFILL_ATTEMPTS = 3;
	private static readonly TimeSpan RETRY_DELAY = TimeSpan.FromDays(14);
	protected async override Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				bool hasUpdates = await BackfillBatch(stoppingToken);
				await Task.Delay(hasUpdates ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(60), stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing embeddings");
				await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
			}
		}
	}

	public record NotesResult(List<NoteResult> Notes);

	public class NoteResult {
		public string Note { get; set; }
		public string Description { get; set; }
		public string Color { get; set; }
	}

	private async Task<bool> BackfillBatch(CancellationToken stoppingToken) {
		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var retryThreshold = DateTime.UtcNow.Add(-RETRY_DELAY);

		var tags = context.Tags
			.IgnoreQueryFilters()
			.Where(t =>
				(string.IsNullOrEmpty(t.Description) || string.IsNullOrEmpty(t.Color)) &&
				t.BackfillAttempts < MAX_BACKFILL_ATTEMPTS &&
				(t.LastBackfillAttempt == null || t.LastBackfillAttempt < retryThreshold)
			)
			.Take(25)
			.ToList();

		if (tags.Count == 0) return false;

		logger.LogInformation("Processing {Count} tags for backfill", tags.Count);

		// Mark all tags as attempted before making the API call
		foreach (var tag in tags) {
			tag.BackfillAttempts++;
			tag.LastBackfillAttempt = DateTime.UtcNow;
		}
		await context.SaveChangesAsync(stoppingToken);

		var systemPrompt = "You are an AI that helps to generate descriptions and colors for perfume notes. " +
			"You will receive a list of notes. For each note, provide a concise description (max 100 characters) and a hex color code that represents the note well. " +
			"Respond in JSON format as an array of objects with 'note', 'description' and 'color' fields.";

		StringBuilder promptBuilder = new();
		promptBuilder.AppendLine("Generate descriptions and colors for these perfume notes:");
		foreach (var tag in tags) {
			promptBuilder.AppendLine($"- {tag.TagName}");
		}

		var chatMessages = new List<OpenAI.Chat.ChatMessage> {
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(promptBuilder.ToString())
		};

		ChatResponseFormat chatResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
			jsonSchemaFormatName: "Notes",
			jsonSchema: BinaryData.FromString("""
				{
					"type": "object",
					"properties": {
						"Notes": {
							"type": "array",
							"items": {
								"type": "object",
								"properties": {
									"Note": { "type": "string" },
									"Description": { "type": "string" },
									"Color": { "type": "string" }
								},
								"required": ["Note", "Description", "Color"],
								"additionalProperties": false
							}
						}
					},
					"required": ["Notes"],
					"additionalProperties": false
				}
				"""),
			jsonSchemaIsStrict: true);


		var options = new ChatCompletionOptions {
			ResponseFormat = chatResponseFormat
		};

		try {
			var completion = await chatClient.CompleteChatAsync(chatMessages, options, stoppingToken);
			var responseContent = completion.Value.Content[0].Text;

			logger.LogInformation("Received response from OpenAI");

			var notes = JsonSerializer.Deserialize<NotesResult>(responseContent);
			int updatedCount = 0;
			foreach (var note in notes!.Notes) {
				var tag = tags.FirstOrDefault(t => t.TagName.Equals(note.Note, StringComparison.OrdinalIgnoreCase));
				if (tag != null) {
					if (string.IsNullOrEmpty(tag.Description) && !string.IsNullOrEmpty(note.Description)) {
						tag.Description = note.Description;
					}
					if (string.IsNullOrEmpty(tag.Color) && !string.IsNullOrEmpty(note.Color)) {
						tag.Color = note.Color;
					}
					updatedCount++;
				}
			}

			await context.SaveChangesAsync(stoppingToken);
			logger.LogInformation("Successfully updated {Count} tags", updatedCount);
			return true;
		} catch (Exception ex) {
			logger.LogError(ex, "Error calling OpenAI or parsing response");
			return false;
		}
	}
}
