using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Features.Perfumes;

public class PerfumeIdentifierBackgroundService(
	IServiceProvider sp,
	ILogger<PerfumeIdentifierBackgroundService> logger) : BackgroundService {

	private const double CONFIDENCE_THRESHOLD = 0.7;
	private const int BATCH_SIZE = 10;
	private const int MIN_TAG_COUNT = 5;

	// Common perfume abbreviations and their full forms
	private static readonly Dictionary<string, string[]> PerfumeAbbreviations = new(StringComparer.OrdinalIgnoreCase) {
		{ "edp", new[] { "eau de parfum" } },
		{ "edt", new[] { "eau de toilette" } },
		{ "edc", new[] { "eau de cologne" } }
	};

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				bool hasUpdates = await ProcessBatch(stoppingToken);
				await Task.Delay(hasUpdates ? TimeSpan.FromSeconds(30) : TimeSpan.FromHours(1), stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing perfume identification backfill");
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
		}
	}

	private async Task<bool> ProcessBatch(CancellationToken cancellationToken) {
		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var perfumeIdentifier = scope.ServiceProvider.GetRequiredService<IPerfumeIdentifier>();

		// Find perfumes that need identification
		var perfumesNeedingIdentification = await context.Perfumes
			.IgnoreQueryFilters()
			.Where(p => !p.IsDeleted
				&& p.PerfumeIdentifiedStatus == Perfume.PerfumeIdentifiedStatuses.NotIdentified
				&& p.MlLeft > 0
				&& (string.IsNullOrEmpty(p.Family) || p.PerfumeTags.Count < MIN_TAG_COUNT))
			.Include(p => p.PerfumeTags)
			.Take(BATCH_SIZE)
			.ToListAsync(cancellationToken);

		if (perfumesNeedingIdentification.Count == 0) {
			return false;
		}

		logger.LogInformation("Processing {Count} perfumes for identification", perfumesNeedingIdentification.Count);

		foreach (var perfume in perfumesNeedingIdentification) {
			try {
				await ProcessPerfume(context, perfume, perfumeIdentifier, cancellationToken);
			} catch (Exception ex) {
				try {
					perfume.PerfumeIdentifiedStatus = Perfume.PerfumeIdentifiedStatuses.IdentificationFailed;
					await context.SaveChangesAsync(cancellationToken);
				} catch (Exception saveEx) {
					logger.LogError(saveEx, "Failed to save IdentifyBackfillError for perfume {PerfumeId} ({House} - {Name})",
						perfume.Id, perfume.House, perfume.PerfumeName);
				}
				logger.LogError(ex, "Failed to identify perfume {PerfumeId} ({House} - {Name})",
					perfume.Id, perfume.House, perfume.PerfumeName);
			}
		}

		return true;
	}

	private async Task ProcessPerfume(
		PerfumeTrackerContext context,
		Perfume perfume,
		IPerfumeIdentifier perfumeIdentifier,
		CancellationToken cancellationToken) {

		logger.LogInformation("Processing perfume {PerfumeId}: {House} - {Name}",
			perfume.Id, perfume.House, perfume.PerfumeName);

		// Step 1: Try to find a match in GlobalPerfumes
		var globalMatch = await FindGlobalPerfumeMatch(context, perfume, cancellationToken);
		if (globalMatch != null) {
			logger.LogInformation("Found global match for {House} - {Name}", perfume.House, perfume.PerfumeName);
			await ApplyGlobalPerfumeData(context, perfume, globalMatch, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
			return;
		}

		// Step 2: Try to identify using LLM
		var identified = await perfumeIdentifier.GetIdentifiedPerfumeAsync(
			perfume.House,
			perfume.PerfumeName,
			perfume.UserId,
			cancellationToken);

		if (identified.ConfidenceScore >= CONFIDENCE_THRESHOLD) {
			logger.LogInformation("LLM identified {House} - {Name} with confidence {Confidence}",
				perfume.House, perfume.PerfumeName, identified.ConfidenceScore);
			await ApplyIdentifiedPerfumeData(context, perfume, identified, cancellationToken);
			await context.SaveChangesAsync(cancellationToken);
			return;
		}

		// Step 3: No good match found, mark as failed
		logger.LogWarning("No good match found for {House} - {Name}, marking as failed",
			perfume.House, perfume.PerfumeName);
		perfume.PerfumeIdentifiedStatus = Perfume.PerfumeIdentifiedStatuses.IdentificationFailed;
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task<GlobalPerfume?> FindGlobalPerfumeMatch(
		PerfumeTrackerContext context,
		Perfume perfume,
		CancellationToken cancellationToken) {

		var normalizedPerfumeHouse = NormalizePerfumeName(perfume.House);
		var normalizedPerfumeName = NormalizePerfumeName(perfume.PerfumeName);

		// Try exact match first
		var exactMatch = await context.GlobalPerfumes
			.Include(gp => gp.GlobalPerfumeTags)
			.ThenInclude(gpt => gpt.GlobalTag)
			.FirstOrDefaultAsync(gp =>
				gp.House.ToLower() == normalizedPerfumeHouse &&
				gp.PerfumeName.ToLower() == normalizedPerfumeName,
				cancellationToken);

		if (exactMatch != null) {
			return exactMatch;
		}

		// Try fuzzy search using full text search
		// Remove all special characters from search text
		var searchText = $"{perfume.House} {perfume.PerfumeName}";
		var cleanedSearchText = System.Text.RegularExpressions.Regex.Replace(searchText, @"[^a-zA-Z0-9\s]", " ");
		var normalized = cleanedSearchText.Trim();
		var tsQuery = string.Join(" & ", normalized
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(t => $"{t}:*"));

		var candidates = await context.GlobalPerfumes
			.Include(gp => gp.GlobalPerfumeTags)
			.ThenInclude(gpt => gpt.GlobalTag)
			.Where(p => p.FullText.Matches(EF.Functions.ToTsQuery(tsQuery)))
			.Take(10) // Increased from 5 to get more candidates
			.ToListAsync(cancellationToken);

		// Find the best match based on string similarity
		GlobalPerfume? bestMatch = null;
		double bestScore = 0;

		foreach (var candidate in candidates) {
			var score = CalculateSimilarity(perfume, candidate, normalizedPerfumeName, normalizedPerfumeHouse);
			if (score > bestScore && score >= 0.75) { // Slightly lowered threshold from 0.8 to 0.75
				bestScore = score;
				bestMatch = candidate;
			}
		}

		if (bestMatch != null) {
			logger.LogInformation("Found fuzzy match for {House} - {Name} -> {GlobalHouse} - {GlobalName} (score: {Score:F2})",
				perfume.House, perfume.PerfumeName, bestMatch.House, bestMatch.PerfumeName, bestScore);
		}

		return bestMatch;
	}

	private string NormalizePerfumeName(string text) {
		var normalized = text.ToLower().Trim();

		// Expand abbreviations to their full forms
		foreach (var abbrev in PerfumeAbbreviations) {
			// Replace the abbreviation with the first full form
			normalized = System.Text.RegularExpressions.Regex.Replace(
				normalized,
				$@"\b{System.Text.RegularExpressions.Regex.Escape(abbrev.Key)}\b",
				abbrev.Value[0],
				System.Text.RegularExpressions.RegexOptions.IgnoreCase);
		}

		return normalized;
	}

	private double CalculateSimilarity(Perfume perfume, GlobalPerfume global, string normalizedPerfumeName, string normalizedPerfumeHouse) {
		// Normalize both strings
		var globalHouse = global.House.ToLower().Trim();
		var globalName = NormalizePerfumeName(global.PerfumeName);

		// House must match (allow for slight variations)
		if (!HousesMatch(normalizedPerfumeHouse, globalHouse)) {
			return 0;
		}

		// Calculate name similarity
		var perfumeWords = normalizedPerfumeName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		var globalWords = globalName.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

		if (perfumeWords.Count == 0 || globalWords.Count == 0) return 0;

		// Jaccard similarity
		var intersection = perfumeWords.Intersect(globalWords).Count();
		var union = perfumeWords.Union(globalWords).Count();
		var jaccardScore = (double)intersection / union;

		// Also calculate a lenient match score (how many words from perfume are in global)
		var coverageScore = (double)intersection / perfumeWords.Count;

		// Use the better of the two scores
		return Math.Max(jaccardScore, coverageScore);
	}

	private bool HousesMatch(string house1, string house2) {
		// Exact match
		if (house1 == house2) return true;

		// One contains the other (e.g., "Chanel" matches "CHANEL Paris")
		if (house1.Contains(house2) || house2.Contains(house1)) return true;

		// Split by spaces and check if main words match
		var words1 = house1.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		var words2 = house2.Split(' ', StringSplitOptions.RemoveEmptyEntries);

		// If either house is a single word, check if it appears in the other
		if (words1.Length == 1 && words2.Any(w => w.Contains(words1[0]))) return true;
		if (words2.Length == 1 && words1.Any(w => w.Contains(words2[0]))) return true;

		return false;
	}

	private async Task ApplyGlobalPerfumeData(
		PerfumeTrackerContext context,
		Perfume perfume,
		GlobalPerfume globalPerfume,
		CancellationToken cancellationToken) {

		// Update family if empty
		if (string.IsNullOrEmpty(perfume.Family) && !string.IsNullOrEmpty(globalPerfume.Family)) {
			perfume.Family = globalPerfume.Family;
		}

		// Add tags from global perfume
		foreach (var globalPerfumeTag in globalPerfume.GlobalPerfumeTags) {
			// Check if user already has this tag
			var userTag = await context.Tags
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(t =>
					t.UserId == perfume.UserId &&
					t.TagName == globalPerfumeTag.GlobalTag.TagName,
					cancellationToken);

			if (userTag == null) {
				// Create user tag from global tag
				userTag = new Tag {
					TagName = globalPerfumeTag.GlobalTag.TagName,
					Color = null,
					UserId = perfume.UserId
				};
				context.Tags.Add(userTag);
				await context.SaveChangesAsync(cancellationToken);
			}

			// Check if perfume already has this tag
			var existingPerfumeTag = await context.PerfumeTags
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(pt =>
					pt.PerfumeId == perfume.Id &&
					pt.TagId == userTag.Id,
					cancellationToken);

			if (existingPerfumeTag == null) {
				context.PerfumeTags.Add(new PerfumeTag {
					PerfumeId = perfume.Id,
					TagId = userTag.Id,
					UserId = perfume.UserId
				});
			}
		}
		perfume.PerfumeIdentifiedStatus = Perfume.PerfumeIdentifiedStatuses.Identified;
	}

	private async Task ApplyIdentifiedPerfumeData(
		PerfumeTrackerContext context,
		Perfume perfume,
		IdentifiedPerfume identified,
		CancellationToken cancellationToken) {

		// Update family if empty
		if (string.IsNullOrEmpty(perfume.Family) && !string.IsNullOrEmpty(identified.Family)) {
			perfume.Family = identified.Family;
		}

		// Add tags from identified notes
		foreach (var note in identified.Notes) {
			if (string.IsNullOrWhiteSpace(note)) continue;

			// Skip if contains special characters (except hyphen) or has more than 2 words
			if (!IsValidTag(note)) {
				logger.LogWarning("Skipping invalid tag '{Tag}' for perfume {House} - {Name}",
					note, perfume.House, perfume.PerfumeName);
				continue;
			}

			// Convert to PascalCase
			var pascalCaseNote = ToPascalCase(note);

			// Check if user already has this tag
			var userTag = await context.Tags
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(t =>
					t.UserId == perfume.UserId &&
					t.TagName == pascalCaseNote,
					cancellationToken);

			if (userTag == null) {
				// Create user tag with a default color
				userTag = new Tag {
					TagName = pascalCaseNote,
					Color = null,
					UserId = perfume.UserId
				};
				context.Tags.Add(userTag);
				await context.SaveChangesAsync(cancellationToken);
			}

			// Check if perfume already has this tag
			var existingPerfumeTag = await context.PerfumeTags
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(pt =>
					pt.PerfumeId == perfume.Id &&
					pt.TagId == userTag.Id,
					cancellationToken);

			if (existingPerfumeTag == null) {
				context.PerfumeTags.Add(new PerfumeTag {
					PerfumeId = perfume.Id,
					TagId = userTag.Id,
					UserId = perfume.UserId
				});
			}
		}
		perfume.PerfumeIdentifiedStatus = Perfume.PerfumeIdentifiedStatuses.Identified;
	}

	private bool IsValidTag(string tag) {
		// Check for special characters (allow letters, numbers, spaces, and hyphens only)
		if (!System.Text.RegularExpressions.Regex.IsMatch(tag, @"^[a-zA-Z0-9\s\-]+$")) {
			return false;
		}

		// Count words (split by space or hyphen)
		var words = tag.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
		if (words.Length > 2) {
			return false;
		}

		return true;
	}

	private string ToPascalCase(string text) {
		if (string.IsNullOrWhiteSpace(text)) return text;

		var words = text.Split(new[] { ' ', '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
		var result = string.Join(" ", words.Select(word =>
			char.ToUpper(word[0]) + word.Substring(1).ToLower()));

		return result;
	}
}
