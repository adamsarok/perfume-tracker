using OpenAI.Chat;
using System.Text;

namespace PerfumeTracker.Server.Features.Tags.Services;

public class TagNoteGroupIdentifier(ChatClient chatClient, PerfumeTrackerContext context) : ITagNoteGroupIdentifier {
	private static readonly JsonSerializerOptions SerializerOptions = new() {
		PropertyNameCaseInsensitive = true
	};

	public async Task<IdentifiedTagNoteGroups> GetIdentifiedTagNoteGroupsAsync(
		IReadOnlyCollection<string> tagNames,
		Guid userId,
		CancellationToken cancellationToken) {

		var distinctTagNames = tagNames
			.Where(t => !string.IsNullOrWhiteSpace(t))
			.Select(t => t.Trim())
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
			.ToList();

		if (distinctTagNames.Count == 0) {
			return new IdentifiedTagNoteGroups([]);
		}

		var promptKeys = distinctTagNames.Select(CreatePromptKey).ToList();
		var cachedCompletions = await context.CachedCompletions
			.IgnoreQueryFilters()
			.Where(cc => cc.UserId == userId
				&& cc.CompletionType == CachedCompletion.CompletionTypes.IdentifyNoteGroup
				&& promptKeys.Contains(cc.Prompt))
			.ToListAsync(cancellationToken);

		var results = new List<IdentifiedTagNoteGroup>();
		var cachedPromptKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var cached in cachedCompletions) {
			var cachedResult = JsonSerializer.Deserialize<IdentifiedTagNoteGroup>(cached.Response, SerializerOptions);
			if (cachedResult == null) continue;

			results.Add(cachedResult);
			cachedPromptKeys.Add(cached.Prompt);
		}

		var uncachedTagNames = distinctTagNames
			.Where(t => !cachedPromptKeys.Contains(CreatePromptKey(t)))
			.ToList();

		if (uncachedTagNames.Count == 0) {
			return new IdentifiedTagNoteGroups(results);
		}

		var completionText = await GetCompletionTextAsync(uncachedTagNames, cancellationToken);
		var identified = JsonSerializer.Deserialize<IdentifiedTagNoteGroups>(completionText, SerializerOptions)
			?? throw new InvalidOperationException("Failed to parse OpenAI note group response");

		foreach (var tag in identified.Tags) {
			if (!uncachedTagNames.Any(t => t.Equals(tag.TagName, StringComparison.OrdinalIgnoreCase))) continue;

			context.CachedCompletions.Add(new CachedCompletion {
				Prompt = CreatePromptKey(tag.TagName),
				CompletionType = CachedCompletion.CompletionTypes.IdentifyNoteGroup,
				Response = JsonSerializer.Serialize(tag),
				CreatedAt = DateTime.UtcNow,
				UserId = userId,
			});
			results.Add(tag);
		}

		await context.SaveChangesAsync(cancellationToken);
		return new IdentifiedTagNoteGroups(results);
	}

	private async Task<string> GetCompletionTextAsync(List<string> tagNames, CancellationToken cancellationToken) {
		var promptBuilder = new StringBuilder();
		promptBuilder.AppendLine("Group these perfume tags into note groups:");
		foreach (var tagName in tagNames) {
			promptBuilder.AppendLine($"- {tagName}");
		}

		var noteGroupList = NoteGroups.ToPromptList();
		var systemPrompt =
			$$"""
			You are a perfume expert. Given perfume tags that may be raw notes, accords, or informal note groups, assign each tag to one canonical note group.

			Use exactly one of these note groups:
			{{noteGroupList}}

			IMPORTANT:
			- Preserve the input tagName exactly.
			- If the input is already a group, map it to the closest canonical group.
			- If the tag is not a perfume note or accord, use Other and set confidenceScore below 0.5.
			- Confidence scale: 0.0 (not confident/not perfume related) to 1.0 (very confident).
			""";

		var messages = new List<OpenAI.Chat.ChatMessage> {
			new SystemChatMessage(systemPrompt),
			new UserChatMessage(promptBuilder.ToString())
		};

		var options = new ChatCompletionOptions {
			ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
				jsonSchemaFormatName: "identified_tag_note_groups",
				jsonSchema: BinaryData.FromString("""
				{
					"type": "object",
					"properties": {
						"Tags": {
							"type": "array",
							"items": {
								"type": "object",
								"properties": {
									"TagName": {
										"type": "string",
										"description": "The exact input tag name"
									},
									"NoteGroup": {
										"type": "string",
										"description": "The canonical note group"
									},
									"ConfidenceScore": {
										"type": "number",
										"description": "Confidence that the note group is correct (0.0 to 1.0)"
									}
								},
								"required": ["TagName", "NoteGroup", "ConfidenceScore"],
								"additionalProperties": false
							}
						}
					},
					"required": ["Tags"],
					"additionalProperties": false
				}
				"""),
				jsonSchemaIsStrict: true)
		};

		var completion = await chatClient.CompleteChatAsync(messages, options, cancellationToken);
		PerfumeTracker.Server.Startup.Diagnostics.RecordChatTokenUsage(completion.Value, "identify_note_group");
		var content = completion.Value.Content;
		if (content is null || content.Count == 0 || string.IsNullOrEmpty(content[0].Text)) {
			throw new InvalidOperationException("OpenAI returned an empty or invalid note group response");
		}

		return content[0].Text;
	}

	private static string CreatePromptKey(string tagName) => $"note-group::{tagName.Trim().ToLowerInvariant()}";
}
