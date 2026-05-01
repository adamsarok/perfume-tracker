using OpenAI.Chat;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Features.Tags;

namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public class ChatAgentTools(PerfumeTrackerContext context, IPerfumeRecommender perfumeRecommender) : IChatAgentTools {
	public static readonly ChatTool SearchOwnedPerfumesByCharacteristicsTool = ChatTool.CreateFunctionTool(
		functionName: "search_owned_perfumes_by_characteristics",
		functionDescription: "Search ONLY perfumes the user already OWNS by simple characteristics like notes, moods, or seasons. LIMITATIONS: (1) Only searches user's OWNED collection, NOT new perfumes. (2) Only handles simple 1-3 word queries like 'vanilla', 'summer night', 'spicy amber'.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {
				"prompt": {
					"type": "string",
					"description": "Simple 1-3 word characteristic to search (e.g., 'vanilla', 'summer', 'woody fresh', 'cozy'). Must be a simple note, mood, or season - NOT a complex question."
				},
				"count": {
					"type": "integer",
					"description": "Number of perfumes to return (1-10)",
					"minimum": 1,
					"maximum": 10,
					"default": 5
				}
			},
			"required": ["prompt"],
			"additionalProperties": false
		}
		""")
	);

	public static readonly ChatTool CheckPerfumeOwnershipTool = ChatTool.CreateFunctionTool(
		functionName: "check_perfume_ownership",
		functionDescription: "Check if user already owns specific perfumes by house and name. Use this BEFORE recommending new perfumes to buy. Returns which perfumes from the list are already owned.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {
				"perfumes": {
					"type": "array",
					"description": "List of perfumes to check ownership for",
					"items": {
						"type": "object",
						"properties": {
							"house": {
								"type": "string",
								"description": "Perfume house/brand name"
							},
							"name": {
								"type": "string",
								"description": "Perfume name"
							}
						},
						"required": ["house", "name"]
					}
				}
			},
			"required": ["perfumes"],
			"additionalProperties": false
		}
		""")
	);

	public static readonly ChatTool AnalyzeWardrobeGapsTool = ChatTool.CreateFunctionTool(
		functionName: "analyze_wardrobe_gaps",
		functionDescription: "Analyze the user's owned perfume wardrobe for underrepresented note groups. Use this when the user asks about wardrobe gaps, missing scent categories, collection balance, or what note groups to explore next.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {},
			"additionalProperties": false
		}
		""")
	);

	public async Task<string> ExecuteToolCall(ChatToolCall toolCall, CancellationToken cancellationToken) {
		var arguments = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(toolCall.FunctionArguments.ToString());
		if (arguments == null) throw new InvalidOperationException("Failed to parse tool arguments");

		switch (toolCall.FunctionName) {
			case "search_owned_perfumes_by_characteristics":
				return await SearchByEmbedding(arguments, cancellationToken);
			case "check_perfume_ownership":
				return await SearchByPerfumesNames(arguments, cancellationToken);
			case "analyze_wardrobe_gaps":
				return await AnalyzeWardrobeGaps(cancellationToken);
			default:
				throw new InvalidOperationException($"Unknown tool: {toolCall.FunctionName}");
		}
	}

	private async Task<string> AnalyzeWardrobeGaps(CancellationToken cancellationToken) {
		var ownedPerfumes = await context.Perfumes
			.AsNoTracking()
			.Where(p => p.MlLeft > 0)
			.Select(p => new {
				p.Id,
				p.House,
				p.PerfumeName,
				p.AverageRating,
				NoteGroups = p.PerfumeTags
					.Where(pt => !pt.IsDeleted
						&& !pt.Tag.IsDeleted
						&& !string.IsNullOrEmpty(pt.Tag.NoteGroup)
						&& pt.Tag.NoteGroup != NoteGroups.Other)
					.Select(pt => pt.Tag.NoteGroup!)
					.Distinct()
					.ToList()
			})
			.ToListAsync(cancellationToken);

		var groupStats = ownedPerfumes
			.SelectMany(p => p.NoteGroups.Select(group => new { Perfume = p, NoteGroup = group }))
			.GroupBy(x => x.NoteGroup)
			.Select(g => new WardrobeNoteGroupCoverage(
				g.Key,
				g.Select(x => x.Perfume.Id).Distinct().Count(),
				g.Select(x => new PerfumeLlmDto(x.Perfume.House, x.Perfume.PerfumeName, x.Perfume.AverageRating))
					.Distinct()
					.OrderByDescending(p => p.Rating)
					.ThenBy(p => p.House)
					.ThenBy(p => p.PerfumeName)
					.Take(5)
					.ToList()))
			.ToDictionary(g => g.NoteGroup, StringComparer.OrdinalIgnoreCase);

		var coverage = NoteGroups.WardrobeCoverage
			.Select(group => groupStats.TryGetValue(group, out var stat)
				? stat
				: new WardrobeNoteGroupCoverage(group, 0, []))
			.OrderBy(g => g.OwnedPerfumeCount)
			.ThenBy(g => g.NoteGroup)
			.ToList();

		var ungroupedPerfumeCount = ownedPerfumes.Count(p => p.NoteGroups.Count == 0);
		var result = new WardrobeGapAnalysis(
			ownedPerfumes.Count,
			coverage.Where(g => g.OwnedPerfumeCount == 0).ToList(),
			coverage.Where(g => g.OwnedPerfumeCount is > 0 and <= 2).ToList(),
			coverage.Where(g => g.OwnedPerfumeCount is >= 3 and <= 5).ToList(),
			coverage.Where(g => g.OwnedPerfumeCount > 5).OrderByDescending(g => g.OwnedPerfumeCount).ToList(),
			groupStats.Values
				.Where(g => !NoteGroups.WardrobeCoverage.Contains(g.NoteGroup, StringComparer.OrdinalIgnoreCase))
				.OrderByDescending(g => g.OwnedPerfumeCount)
				.ToList(),
			ungroupedPerfumeCount);

		return JsonSerializer.Serialize(result, new JsonSerializerOptions {
			WriteIndented = false
		});
	}

	private async Task<string> SearchByPerfumesNames(Dictionary<string, JsonElement> arguments, CancellationToken cancellationToken) {
		var perfumesElement = arguments["perfumes"];
		var perfumesToCheck = perfumesElement.Deserialize<List<PerfumeOwnershipCheckQuery>>(new JsonSerializerOptions {
			PropertyNameCaseInsensitive = true
		});
		if (perfumesToCheck == null) throw new ArgumentException("Missing perfumes");


		var ownedPerfumes = await context.Perfumes
			.AsNoTracking()
			.Select(r => new PerfumeLlmDto(r.House, r.PerfumeName, r.AverageRating))
			.ToListAsync(cancellationToken);

		var results = perfumesToCheck.Select<PerfumeOwnershipCheckQuery, PerfumeOwnershipCheckResult>(check => {
			var exactMatch = ownedPerfumes.FirstOrDefault(owned =>
				owned.House.Equals(check.House, StringComparison.OrdinalIgnoreCase) &&
				owned.PerfumeName.Equals(check.Name, StringComparison.OrdinalIgnoreCase));

			if (exactMatch != null) return new PerfumeOwnershipCheckResult(check.House, check.Name, true);

			var fuzzyMatch = ownedPerfumes
				.Select(owned => new {
					owned.House,
					owned.PerfumeName,
					owned.Rating,
					houseScore = CalculateSimilarity(check.House.ToLowerInvariant(), owned.House.ToLowerInvariant()),
					nameScore = CalculateSimilarity(check.Name.ToLowerInvariant(), owned.PerfumeName.ToLowerInvariant())
				})
				.Where(m => (m.houseScore > 0.7 && m.nameScore > 0.7))
				.OrderByDescending(m => m.houseScore + m.nameScore)
				.FirstOrDefault();

			if (fuzzyMatch != null) return new PerfumeOwnershipCheckResult(check.House, check.Name, true);

			return new PerfumeOwnershipCheckResult(check.House, check.Name, false);
		}).ToList();

		return JsonSerializer.Serialize(results, new JsonSerializerOptions {
			WriteIndented = false
		});
	}

	private async Task<string> SearchByEmbedding(Dictionary<string, JsonElement> arguments, CancellationToken cancellationToken) {
		var prompt = arguments["prompt"].GetString() ?? throw new ArgumentException("Missing prompt");
		var count = arguments.TryGetValue("count", out var countElement) ? countElement.GetInt32() : 5;
		var recommendations = await perfumeRecommender.GetRecommendationsForOccasionMoodPrompt(count, prompt, cancellationToken);
		var perfumes = recommendations.Select(r => new PerfumeLlmDto(r.Perfume.Perfume.House, r.Perfume.Perfume.PerfumeName, r.Perfume.Perfume.AverageRating));
		return JsonSerializer.Serialize(perfumes, new JsonSerializerOptions {
			WriteIndented = false
		});
	}

	private static double CalculateSimilarity(string source, string target) {
		if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target)) return 0;
		if (source == target) return 1.0;

		var sourceLength = source.Length;
		var targetLength = target.Length;
		var distance = LevenshteinDistance(source, target);

		return 1.0 - (double)distance / Math.Max(sourceLength, targetLength);
	}

	private static int LevenshteinDistance(string source, string target) {
		if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
		if (string.IsNullOrEmpty(target)) return source.Length;

		var sourceLength = source.Length;
		var targetLength = target.Length;
		var distance = new int[sourceLength + 1, targetLength + 1];

		for (var i = 0; i <= sourceLength; distance[i, 0] = i++) { }
		for (var j = 0; j <= targetLength; distance[0, j] = j++) { }

		for (var i = 1; i <= sourceLength; i++) {
			for (var j = 1; j <= targetLength; j++) {
				var cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
				distance[i, j] = Math.Min(
					Math.Min(distance[i - 1, j] + 1, distance[i, j - 1] + 1),
					distance[i - 1, j - 1] + cost);
			}
		}

		return distance[sourceLength, targetLength];
	}

}

public record WardrobeGapAnalysis(
	int OwnedPerfumeCount,
	List<WardrobeNoteGroupCoverage> MissingNoteGroups,
	List<WardrobeNoteGroupCoverage> ThinNoteGroups,
	List<WardrobeNoteGroupCoverage> BalancedNoteGroups,
	List<WardrobeNoteGroupCoverage> StrongNoteGroups,
	List<WardrobeNoteGroupCoverage> NonCoreNoteGroups,
	int UngroupedPerfumeCount);

public record WardrobeNoteGroupCoverage(
	string NoteGroup,
	int OwnedPerfumeCount,
	List<PerfumeLlmDto> ExamplePerfumes);
