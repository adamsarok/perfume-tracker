using PerfumeTracker.Server.Features.Perfumes.Extensions;

namespace PerfumeTracker.Server.Features.Embedding;

public class EmbeddingBackgroundService(IServiceProvider sp, ILogger<EmbeddingBackgroundService> logger, IEncoder encoder) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		if (encoder is NullEncoder) {
			logger.LogInformation("Embedding service disabled (no OpenAI API key configured)");
			return;
		}

		await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				bool hasUpdates = await BackfillBatch(stoppingToken);
				await Task.Delay(hasUpdates ? TimeSpan.FromSeconds(5) : TimeSpan.FromMinutes(60), stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing embeddings");
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
		}
	}

	/// <summary>
	/// Backfill all perfumes without embeddings
	/// </summary>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	private async Task<bool> BackfillBatch(CancellationToken cancellationToken) {
		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		// Find perfumes that need embeddings
		var perfumesNeedingEmbedding = await context.Perfumes
			.IgnoreQueryFilters()
			.Where(p => !context.PerfumeDocuments.Any(d => d.Id == p.Id) && !p.IsDeleted)
			.Take(100)
			.Include(p => p.PerfumeTags).ThenInclude(pt => pt.Tag)
			.Include(p => p.PerfumeRatings)
			.Include(p => p.PerfumeEvents)
			.ToListAsync(cancellationToken);

		if (perfumesNeedingEmbedding.Count == 0) return false;

		bool hasUpdates = false;

		logger.LogInformation("Processing {Count} perfumes for embeddings", perfumesNeedingEmbedding.Count);

		foreach (var perfume in perfumesNeedingEmbedding) {
			try {
				var text = perfume.GetTextForEmbedding();
				if (string.IsNullOrWhiteSpace(text)) {
					continue; // TODO mark so we don't try again?
				}
				var embedding = await encoder.GetEmbeddings(text, cancellationToken);

				context.PerfumeDocuments.Add(new PerfumeDocument {
					Id = perfume.Id,
					Text = text,
					Embedding = embedding,
					UserId = perfume.UserId
				});

				await context.SaveChangesAsync(cancellationToken);
				hasUpdates = true;
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to generate embedding for perfume {PerfumeId}", perfume.Id);
			}
		}
		return hasUpdates;
	}
}