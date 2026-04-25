using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Features.Perfumes;

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
			return false;
		}

		logger.LogInformation("Processing {Count} perfumes for parfumeur identification", perfumesNeedingParfumeur.Count);

		foreach (var perfume in perfumesNeedingParfumeur) {
			try {
				await ProcessPerfume(context, perfume, parfumeurIdentifier, cancellationToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to identify parfumeur for perfume {PerfumeId} ({House} - {Name})",
					perfume.Id, perfume.House, perfume.PerfumeName);
			}
		}

		return true;
	}

	private async Task ProcessPerfume(
		PerfumeTrackerContext context,
		Perfume perfume,
		IParfumeurIdentifier parfumeurIdentifier,
		CancellationToken cancellationToken) {

		logger.LogInformation("Processing parfumeur for perfume {PerfumeId}: {House} - {Name}",
			perfume.Id, perfume.House, perfume.PerfumeName);

		var identified = await parfumeurIdentifier.GetIdentifiedParfumeurAsync(
			perfume.House,
			perfume.PerfumeName,
			perfume.UserId,
			cancellationToken);

		if (identified.ConfidenceScore < CONFIDENCE_THRESHOLD || string.IsNullOrEmpty(identified.Parfumeur)) {
			perfume.Parfumeur = UNKNOWN_PARFUMEUR;
			await context.SaveChangesAsync(cancellationToken);
			logger.LogInformation("Marked parfumeur as {Parfumeur} for {House} - {Name}; confidence {Confidence:P0} below threshold",
				UNKNOWN_PARFUMEUR,
				perfume.House, perfume.PerfumeName, identified.ConfidenceScore);
			return;
		}

		var parfumeur = identified.Parfumeur.Trim();
		if (string.IsNullOrWhiteSpace(parfumeur)) {
			perfume.Parfumeur = UNKNOWN_PARFUMEUR;
			await context.SaveChangesAsync(cancellationToken);
			logger.LogWarning("Marked parfumeur as {Parfumeur} for {House} - {Name}; response was empty",
				UNKNOWN_PARFUMEUR,
				perfume.House, perfume.PerfumeName);
			return;
		}

		if (parfumeur.Length > 250) {
			perfume.Parfumeur = UNKNOWN_PARFUMEUR;
			await context.SaveChangesAsync(cancellationToken);
			logger.LogWarning("Marked parfumeur as {Parfumeur} for {House} - {Name}; parfumeur exceeds max length",
				UNKNOWN_PARFUMEUR,
				perfume.House, perfume.PerfumeName);
			return;
		}

		perfume.Parfumeur = parfumeur;
		await context.SaveChangesAsync(cancellationToken);
		logger.LogInformation("Updated parfumeur for {House} - {Name} to {Parfumeur}",
			perfume.House, perfume.PerfumeName, parfumeur);
	}
}
