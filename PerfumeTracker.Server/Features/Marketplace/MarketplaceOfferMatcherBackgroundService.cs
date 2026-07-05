using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Features.Marketplace;

using PerfumeTracker.Server.Startup;
using System.Diagnostics;

public class MarketplaceOfferMatcherBackgroundService(
	IServiceProvider sp,
	ILogger<MarketplaceOfferMatcherBackgroundService> logger) : BackgroundService {

	private const double CONFIDENCE_THRESHOLD = 0.75;
	private const int BATCH_SIZE = 100;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Delay(TimeSpan.FromSeconds(20), stoppingToken);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				var hasUpdates = await ProcessBatch(stoppingToken);
				await Task.Delay(hasUpdates ? TimeSpan.FromSeconds(30) : TimeSpan.FromHours(1), stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing marketplace offer matching");
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
		}
	}

	private async Task<bool> ProcessBatch(CancellationToken cancellationToken) {
		using var activity = Diagnostics.ActivitySource.StartActivity("marketplace_offer.matching", ActivityKind.Internal);
		activity?.SetTag("job.name", nameof(MarketplaceOfferMatcherBackgroundService));

		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var offers = await context.MarketplaceOffers
			.IgnoreQueryFilters()
			.Where(o => !o.IsDeleted
				&& o.MatchConfidence == null
				&& o.BrandName != null
				&& o.ProductName != "")
			.OrderBy(o => o.ImportedAt)
			.Take(BATCH_SIZE)
			.ToListAsync(cancellationToken);

		activity?.SetTag("offer.count", offers.Count);
		if (offers.Count == 0) return false;

		logger.LogInformation("Processing {Count} marketplace offers for owned-perfume matching", offers.Count);

		var userIds = offers.Select(o => o.UserId).Distinct().ToList();
		var perfumes = await context.Perfumes
			.IgnoreQueryFilters()
			.Where(p => !p.IsDeleted && userIds.Contains(p.UserId))
			.Select(p => new {
				p.Id,
				p.UserId,
				p.House,
				p.PerfumeName
			})
			.ToListAsync(cancellationToken);

		var perfumesByUser = perfumes
			.GroupBy(p => p.UserId)
			.ToDictionary(g => g.Key, g => g.ToList());

		var matchedCount = 0;
		foreach (var offer in offers) {
			if (!perfumesByUser.TryGetValue(offer.UserId, out var userPerfumes)) {
				offer.MatchConfidence = 0;
				continue;
			}

			var bestMatch = userPerfumes
				.Select(p => new {
					Perfume = p,
					Score = PerfumeNameMatcher.CalculateSimilarity(
						offer.BrandName!,
						offer.ProductName,
						p.House,
						p.PerfumeName)
				})
				.OrderByDescending(m => m.Score)
				.FirstOrDefault();

			var bestScore = bestMatch?.Score ?? 0;
			offer.MatchConfidence = (decimal)bestScore;

			if (bestMatch != null && bestScore >= CONFIDENCE_THRESHOLD) {
				offer.MatchedPerfumeId = bestMatch.Perfume.Id;
				matchedCount++;
			}
		}

		await context.SaveChangesAsync(cancellationToken);

		activity?.SetTag("offer.matched_count", matchedCount);
		logger.LogInformation("Matched {MatchedCount} marketplace offers", matchedCount);
		return true;
	}
}
