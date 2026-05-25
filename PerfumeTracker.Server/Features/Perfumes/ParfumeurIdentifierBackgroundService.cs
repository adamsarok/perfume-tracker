using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Features.Perfumes;

using PerfumeTracker.Server.Startup;
using System.Diagnostics;

public class ParfumeurIdentifierBackgroundService(
	IServiceProvider sp,
	ILogger<ParfumeurIdentifierBackgroundService> logger) : BackgroundService {

	private const string UNKNOWN_PARFUMEUR = "Unknown";
	private const double CONFIDENCE_THRESHOLD = 0.7;
	private const int BATCH_SIZE = 10;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
		logger.LogInformation("Starting parfumeur identification backfill");

		try {
			while (!stoppingToken.IsCancellationRequested && await ProcessBatch(stoppingToken)) { }
			logger.LogInformation("Completed parfumeur identification backfill");
		} catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) {
			logger.LogInformation("Parfumeur identification backfill cancelled");
		} catch (Exception ex) {
			logger.LogError(ex, "Error processing parfumeur identification backfill");
		}
	}

	private async Task<bool> ProcessBatch(CancellationToken cancellationToken) {
		using var activity = Diagnostics.ActivitySource.StartActivity("parfumeur_identification.backfill", ActivityKind.Internal);
		activity?.SetTag("job.name", nameof(ParfumeurIdentifierBackgroundService));

		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var parfumeurIdentifier = scope.ServiceProvider.GetRequiredService<IParfumeurIdentifier>();

		var perfumesNeedingParfumeur = await context.Perfumes
			.IgnoreQueryFilters()
			.Where(p => !p.IsDeleted
				&& p.Ml > 0
				&& string.IsNullOrEmpty(p.Parfumeur))
			.OrderBy(p => p.Id)
			.Take(BATCH_SIZE)
			.ToListAsync(cancellationToken);

		if (perfumesNeedingParfumeur.Count == 0) {
			activity?.SetTag("perfume.count", 0);
			return false;
		}

		activity?.SetTag("perfume.count", perfumesNeedingParfumeur.Count);
		logger.LogInformation("Processing {Count} perfumes for parfumeur identification", perfumesNeedingParfumeur.Count);

		var failedCount = 0;
		foreach (var perfume in perfumesNeedingParfumeur) {
			try {
				await ProcessPerfume(context, perfume, parfumeurIdentifier, cancellationToken);
			} catch (Exception ex) {
				failedCount++;
				logger.LogError(ex, "Failed to identify parfumeur for perfume {PerfumeId} ({House} - {Name})",
					perfume.Id, perfume.House, perfume.PerfumeName);
			}
		}

		activity?.SetTag("perfume.failed_count", failedCount);
		return true;
	}

	private async Task ProcessPerfume(
		PerfumeTrackerContext context,
		Perfume perfume,
		IParfumeurIdentifier parfumeurIdentifier,
		CancellationToken cancellationToken) {

		using var activity = Diagnostics.ActivitySource.StartActivity("parfumeur_identification.backfill.perfume", ActivityKind.Internal);
		activity?.SetTag("perfume.id", perfume.Id);

		logger.LogInformation("Processing parfumeur for perfume {PerfumeId}: {House} - {Name}",
			perfume.Id, perfume.House, perfume.PerfumeName);

		var identified = await parfumeurIdentifier.GetIdentifiedParfumeurAsync(
			perfume.House,
			perfume.PerfumeName,
			perfume.UserId,
			cancellationToken);

		activity?.SetTag("parfumeur.identification.confidence", identified.ConfidenceScore);
		if (identified.ConfidenceScore < CONFIDENCE_THRESHOLD || string.IsNullOrEmpty(identified.Parfumeur)) {
			activity?.SetTag("parfumeur.identification.result", "unknown");
			perfume.Parfumeur = UNKNOWN_PARFUMEUR;
			await context.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Marked parfumeur as {Parfumeur} for {House} - {Name}; confidence {Confidence:P0} below threshold",
				UNKNOWN_PARFUMEUR,
				perfume.House, perfume.PerfumeName, identified.ConfidenceScore);
			return;
		}

		var parfumeur = identified.Parfumeur.Trim();
		if (string.IsNullOrWhiteSpace(parfumeur)) {
			activity?.SetTag("parfumeur.identification.result", "empty");
			perfume.Parfumeur = UNKNOWN_PARFUMEUR;
			await context.SaveChangesAsync(cancellationToken);
			logger.LogWarning("Marked parfumeur as {Parfumeur} for {House} - {Name}; response was empty",
				UNKNOWN_PARFUMEUR,
				perfume.House, perfume.PerfumeName);
			return;
		}

		if (parfumeur.Length > 250) {
			activity?.SetTag("parfumeur.identification.result", "too_long");
			perfume.Parfumeur = UNKNOWN_PARFUMEUR;
			await context.SaveChangesAsync(cancellationToken);
			logger.LogWarning("Marked parfumeur as {Parfumeur} for {House} - {Name}; parfumeur exceeds max length",
				UNKNOWN_PARFUMEUR,
				perfume.House, perfume.PerfumeName);
			return;
		}

		perfume.Parfumeur = parfumeur;
		await context.SaveChangesAsync(cancellationToken);
		activity?.SetTag("parfumeur.identification.result", "identified");
		logger.LogInformation("Updated parfumeur for {House} - {Name} to {Parfumeur}",
			perfume.House, perfume.PerfumeName, parfumeur);
	}
}
