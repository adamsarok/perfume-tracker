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
					"description": "Number of perfumes to return (1-25)",
					"minimum": 1,
					"maximum": 25,
					"default": 5
				}
			},
			"required": ["prompt"],
			"additionalProperties": false
		}
		""")
	);

	public static readonly ChatTool FilterOwnedPerfumesTool = ChatTool.CreateFunctionTool(
		functionName: "filter_owned_perfumes",
		functionDescription: "Filter and order ONLY perfumes the user already OWNS by structured collection facts. Use this for least worn, highest rated, not worn recently, never worn, specific tags, family, house, or availability queries.",
		functionParameters: BinaryData.FromString("""
		{
			"type": "object",
			"properties": {
				"count": {
					"type": "integer",
					"description": "Number of perfumes to return (1-50)",
					"minimum": 1,
					"maximum": 50,
					"default": 10
				},
				"orderBy": {
					"type": "string",
					"description": "Field to order by",
					"enum": ["rating", "wearCount", "lastWorn", "mlLeft", "house", "perfumeName"],
					"default": "rating"
				},
				"orderDirection": {
					"type": "string",
					"description": "Sort direction",
					"enum": ["asc", "desc"],
					"default": "desc"
				},
				"minRating": {
					"type": "number",
					"description": "Minimum user rating, inclusive"
				},
				"maxRating": {
					"type": "number",
					"description": "Maximum user rating, inclusive"
				},
				"minWearCount": {
					"type": "integer",
					"description": "Minimum wear count, inclusive"
				},
				"maxWearCount": {
					"type": "integer",
					"description": "Maximum wear count, inclusive"
				},
				"notWornInDays": {
					"type": "integer",
					"description": "Return perfumes never worn or not worn in at least this many days"
				},
				"wornWithinDays": {
					"type": "integer",
					"description": "Return perfumes worn within this many days"
				},
				"neverWorn": {
					"type": "boolean",
					"description": "When true, return only perfumes with zero wears"
				},
				"onlyAvailable": {
					"type": "boolean",
					"description": "When true, only return perfumes with ml left",
					"default": true
				},
				"family": {
					"type": "string",
					"description": "Case-insensitive family match"
				},
				"house": {
					"type": "string",
					"description": "Case-insensitive house match"
				},
				"tagsAny": {
					"type": "array",
					"description": "Return perfumes with at least one of these tags",
					"items": { "type": "string" }
				},
				"tagsAll": {
					"type": "array",
					"description": "Return perfumes with all of these tags",
					"items": { "type": "string" }
				}
			},
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
			case "filter_owned_perfumes":
				return await FilterOwnedPerfumes(arguments, cancellationToken);
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
				p.LatestRating,
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
				g.Select(x => new PerfumeLlmDto(
						x.Perfume.Id,
						x.Perfume.House,
						x.Perfume.PerfumeName,
						string.Empty,
						x.Perfume.LatestRating,
						0,
						null,
						0,
						[],
						null))
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
			.Select(r => new { r.House, r.PerfumeName, r.LatestRating })
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
					owned.LatestRating,
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
		count = Math.Clamp(count, 1, 25);
		var recommendations = await perfumeRecommender.GetRecommendationsForOccasionMoodPrompt(count, prompt, cancellationToken);
		var perfumes = recommendations.Select(r => ToPerfumeLlmDto(r.Perfume));
		return JsonSerializer.Serialize(perfumes, new JsonSerializerOptions {
			WriteIndented = false
		});
	}

	private async Task<string> FilterOwnedPerfumes(Dictionary<string, JsonElement> arguments, CancellationToken cancellationToken) {
		var count = GetOptionalInt(arguments, "count") ?? 10;
		count = Math.Clamp(count, 1, 50);
		var orderBy = GetOptionalString(arguments, "orderBy") ?? "rating";
		var orderDirection = GetOptionalString(arguments, "orderDirection") ?? "desc";
		var descending = !orderDirection.Equals("asc", StringComparison.OrdinalIgnoreCase);
		var onlyAvailable = GetOptionalBool(arguments, "onlyAvailable") ?? true;

		var query = context.Perfumes.AsNoTracking();

		if (onlyAvailable) query = query.Where(p => p.MlLeft > 0);
		if (GetOptionalDecimal(arguments, "minRating") is { } minRating) query = query.Where(p => p.LatestRating >= minRating);
		if (GetOptionalDecimal(arguments, "maxRating") is { } maxRating) query = query.Where(p => p.LatestRating <= maxRating);
		if (GetOptionalInt(arguments, "minWearCount") is { } minWearCount) query = query.Where(p => p.WearCount >= minWearCount);
		if (GetOptionalInt(arguments, "maxWearCount") is { } maxWearCount) query = query.Where(p => p.WearCount <= maxWearCount);
		if (GetOptionalBool(arguments, "neverWorn") == true) query = query.Where(p => p.WearCount == 0);
		if (GetOptionalInt(arguments, "notWornInDays") is { } notWornInDays) {
			var cutoff = DateTime.UtcNow.AddDays(-notWornInDays);
			query = query.Where(p => p.LastWorn == null || p.LastWorn <= cutoff);
		}
		if (GetOptionalInt(arguments, "wornWithinDays") is { } wornWithinDays) {
			var cutoff = DateTime.UtcNow.AddDays(-wornWithinDays);
			query = query.Where(p => p.LastWorn != null && p.LastWorn >= cutoff);
		}
		if (GetOptionalString(arguments, "family") is { } family) {
			var normalizedFamily = family.ToLower();
			query = query.Where(p => p.Family.ToLower() == normalizedFamily);
		}
		if (GetOptionalString(arguments, "house") is { } house) {
			var normalizedHouse = house.ToLower();
			query = query.Where(p => p.House.ToLower() == normalizedHouse);
		}

		var tagsAny = GetOptionalStringList(arguments, "tagsAny");
		if (tagsAny.Count > 0) {
			var normalizedTagsAny = tagsAny.Select(t => t.ToLower()).ToList();
			query = query.Where(p => p.PerfumeTags.Any(pt => normalizedTagsAny.Contains(pt.Tag.TagName.ToLower())));
		}

		var tagsAll = GetOptionalStringList(arguments, "tagsAll");
		foreach (var tag in tagsAll) {
			var normalizedTag = tag.ToLower();
			query = query.Where(p => p.PerfumeTags.Any(pt => pt.Tag.TagName.ToLower() == normalizedTag));
		}

		query = ApplyOrdering(query, orderBy, descending);

		var perfumes = await query
			.Include(p => p.PerfumeTags).ThenInclude(pt => pt.Tag)
			.Include(p => p.PerfumeRatings)
			.Take(count)
			.ToListAsync(cancellationToken);

		return JsonSerializer.Serialize(perfumes.Select(ToPerfumeLlmDto), new JsonSerializerOptions {
			WriteIndented = false
		});
	}

	private static IQueryable<Perfume> ApplyOrdering(IQueryable<Perfume> query, string orderBy, bool descending) {
		return orderBy.ToLowerInvariant() switch {
			"wearcount" => descending
				? query.OrderByDescending(p => p.WearCount).ThenByDescending(p => p.LatestRating).ThenBy(p => p.House).ThenBy(p => p.PerfumeName)
				: query.OrderBy(p => p.WearCount).ThenByDescending(p => p.LatestRating).ThenBy(p => p.House).ThenBy(p => p.PerfumeName),
			"lastworn" => descending
				? query.OrderByDescending(p => p.LastWorn).ThenByDescending(p => p.LatestRating).ThenBy(p => p.House).ThenBy(p => p.PerfumeName)
				: query.OrderBy(p => p.LastWorn == null ? 0 : 1).ThenBy(p => p.LastWorn).ThenByDescending(p => p.LatestRating).ThenBy(p => p.House).ThenBy(p => p.PerfumeName),
			"mlleft" => descending
				? query.OrderByDescending(p => p.MlLeft).ThenByDescending(p => p.LatestRating).ThenBy(p => p.House).ThenBy(p => p.PerfumeName)
				: query.OrderBy(p => p.MlLeft).ThenByDescending(p => p.LatestRating).ThenBy(p => p.House).ThenBy(p => p.PerfumeName),
			"house" => descending
				? query.OrderByDescending(p => p.House).ThenBy(p => p.PerfumeName)
				: query.OrderBy(p => p.House).ThenBy(p => p.PerfumeName),
			"perfumename" => descending
				? query.OrderByDescending(p => p.PerfumeName).ThenBy(p => p.House)
				: query.OrderBy(p => p.PerfumeName).ThenBy(p => p.House),
			_ => descending
				? query.OrderByDescending(p => p.LatestRating).ThenBy(p => p.WearCount).ThenBy(p => p.House).ThenBy(p => p.PerfumeName)
				: query.OrderBy(p => p.LatestRating).ThenBy(p => p.WearCount).ThenBy(p => p.House).ThenBy(p => p.PerfumeName)
		};
	}

	private static PerfumeLlmDto ToPerfumeLlmDto(Perfume perfume) {
		var lastComment = perfume.PerfumeRatings
			.Where(r => !string.IsNullOrWhiteSpace(r.Comment))
			.OrderByDescending(r => r.RatingDate)
			.Select(r => r.Comment)
			.FirstOrDefault();

		return new PerfumeLlmDto(
			perfume.Id,
			perfume.House,
			perfume.PerfumeName,
			perfume.Family,
			perfume.LatestRating,
			perfume.WearCount,
			perfume.LastWorn,
			perfume.MlLeft,
			perfume.PerfumeTags
				.Select(pt => pt.Tag.TagName)
				.Distinct()
				.OrderBy(t => t)
				.ToList(),
			lastComment);
	}

	private static PerfumeLlmDto ToPerfumeLlmDto(PerfumeWithWornStatsDto perfume) {
		return new PerfumeLlmDto(
			perfume.Perfume.Id,
			perfume.Perfume.House,
			perfume.Perfume.PerfumeName,
			perfume.Perfume.Family,
			perfume.Perfume.LatestRating,
			perfume.Perfume.WearCount,
			perfume.Perfume.LastWorn,
			perfume.Perfume.MlLeft,
			perfume.Perfume.Tags.Select(t => t.TagName).OrderBy(t => t).ToList(),
			string.IsNullOrWhiteSpace(perfume.LastComment) ? null : perfume.LastComment);
	}

	private static string? GetOptionalString(Dictionary<string, JsonElement> arguments, string key) {
		return arguments.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.String
			? element.GetString()
			: null;
	}

	private static int? GetOptionalInt(Dictionary<string, JsonElement> arguments, string key) {
		return arguments.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value)
			? value
			: null;
	}

	private static decimal? GetOptionalDecimal(Dictionary<string, JsonElement> arguments, string key) {
		return arguments.TryGetValue(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out var value)
			? value
			: null;
	}

	private static bool? GetOptionalBool(Dictionary<string, JsonElement> arguments, string key) {
		return arguments.TryGetValue(key, out var element) && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
			? element.GetBoolean()
			: null;
	}

	private static List<string> GetOptionalStringList(Dictionary<string, JsonElement> arguments, string key) {
		if (!arguments.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Array) return [];

		return element.EnumerateArray()
			.Where(e => e.ValueKind == JsonValueKind.String)
			.Select(e => e.GetString())
			.Where(s => !string.IsNullOrWhiteSpace(s))
			.Select(s => s!)
			.ToList();
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
