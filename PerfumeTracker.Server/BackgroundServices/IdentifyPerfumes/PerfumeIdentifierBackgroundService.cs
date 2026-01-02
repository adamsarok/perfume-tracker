using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.BackgroundServices.IdentifyPerfumes;

public class PerfumeIdentifierBackgroundService(
	IServiceProvider sp,
	ILogger<PerfumeIdentifierBackgroundService> logger) : BackgroundService {

	private const double CONFIDENCE_THRESHOLD = 0.7;
	private const int BATCH_SIZE = 10;
	private const int MIN_TAG_COUNT = 5;

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
				&& !p.IsIdentifyBackfillFailed
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
		perfume.IsIdentifyBackfillFailed = true;
		await context.SaveChangesAsync(cancellationToken);
	}

	private async Task<GlobalPerfume?> FindGlobalPerfumeMatch(
		PerfumeTrackerContext context,
		Perfume perfume,
		CancellationToken cancellationToken) {

		// Try exact match first
		var exactMatch = await context.GlobalPerfumes
			.Include(gp => gp.GlobalPerfumeTags)
			.ThenInclude(gpt => gpt.GlobalTag)
			.FirstOrDefaultAsync(gp =>
				gp.House.ToLower() == perfume.House.ToLower() &&
				gp.PerfumeName.ToLower() == perfume.PerfumeName.ToLower(),
				cancellationToken);

		if (exactMatch != null) {
			return exactMatch;
		}

		// Try fuzzy search using full text search
		var searchText = $"{perfume.House} {perfume.PerfumeName}";
		var normalized = searchText.Trim();
		var tsQuery = string.Join(" & ", normalized
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(t => $"{t}:*"));

		var candidates = await context.GlobalPerfumes
			.Include(gp => gp.GlobalPerfumeTags)
			.ThenInclude(gpt => gpt.GlobalTag)
			.Where(p => p.FullText.Matches(EF.Functions.ToTsQuery(tsQuery)))
			.Take(5)
			.ToListAsync(cancellationToken);

		// Find the best match based on string similarity
		GlobalPerfume? bestMatch = null;
		double bestScore = 0;

		foreach (var candidate in candidates) {
			var score = CalculateSimilarity(perfume, candidate);
			if (score > bestScore && score >= 0.8) { // 80% similarity threshold
				bestScore = score;
				bestMatch = candidate;
			}
		}

		return bestMatch;
	}

	private double CalculateSimilarity(Perfume perfume, GlobalPerfume global) {
		var perfumeStr = $"{perfume.House} {perfume.PerfumeName}".ToLower();
		var globalStr = $"{global.House} {global.PerfumeName}".ToLower();

		// Simple similarity calculation based on common words
		var perfumeWords = perfumeStr.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
		var globalWords = globalStr.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

		if (perfumeWords.Count == 0 || globalWords.Count == 0) return 0;

		var intersection = perfumeWords.Intersect(globalWords).Count();
		var union = perfumeWords.Union(globalWords).Count();

		return (double)intersection / union;
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
				.FirstOrDefaultAsync(t =>
					t.UserId == perfume.UserId &&
					t.TagName == globalPerfumeTag.GlobalTag.TagName,
					cancellationToken);

			if (userTag == null) {
				// Create user tag from global tag
				userTag = new Tag {
					TagName = globalPerfumeTag.GlobalTag.TagName,
					Color = globalPerfumeTag.GlobalTag.Color,
					UserId = perfume.UserId
				};
				context.Tags.Add(userTag);
				await context.SaveChangesAsync(cancellationToken);
			}

			// Check if perfume already has this tag
			var existingPerfumeTag = await context.PerfumeTags
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

			// Check if user already has this tag
			var userTag = await context.Tags
				.FirstOrDefaultAsync(t =>
					t.UserId == perfume.UserId &&
					t.TagName == note,
					cancellationToken);

			if (userTag == null) {
				// Create user tag with a default color
				userTag = new Tag {
					TagName = note,
					Color = GenerateColorForTag(note),
					UserId = perfume.UserId
				};
				context.Tags.Add(userTag);
				await context.SaveChangesAsync(cancellationToken);
			}

			// Check if perfume already has this tag
			var existingPerfumeTag = await context.PerfumeTags
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
	}

	private string GenerateColorForTag(string tagName) {
		// Generate a consistent color based on tag name hash
		var hash = tagName.GetHashCode();
		var r = (hash & 0xFF0000) >> 16;
		var g = (hash & 0x00FF00) >> 8;
		var b = hash & 0x0000FF;

		// Ensure colors are not too dark
		r = Math.Max(r, 100);
		g = Math.Max(g, 100);
		b = Math.Max(b, 100);

		return $"#{r:X2}{g:X2}{b:X2}";
	}
}
