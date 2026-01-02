using System.Text;

namespace PerfumeTracker.Server.Services.Embedding;

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
				var text = BuildText(perfume);
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

	private string BuildText(Perfume perfume) {
		var sb = new StringBuilder(); // only add fields which are not available in perfume. Eg House can be searched in Perfume - sentiment based on all tags & comments can not

		if (perfume.PerfumeTags.Any()) {
			sb.Append($"Notes: {string.Join(", ", perfume.PerfumeTags.Select(pt => pt.Tag.TagName))}. ");
		}

		if (perfume.PerfumeRatings.Any()) {
			sb.Append($"Rating: {perfume.PerfumeRatings.Average(x => x.Rating).ToString("0.#")}/10 points. ");
			sb.Append($"User comments: ");
			var comments = perfume.PerfumeRatings
				.Where(r => !string.IsNullOrWhiteSpace(r.Comment))
				.OrderByDescending(r => r.UpdatedAt)
				.Take(10)
				.Select(r => r.Comment);
			if (comments.Any()) {
				sb.Append($"{string.Join(". ", comments)}. ");
			}
		}

		var wearEvents = perfume.PerfumeEvents.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn);
		if (wearEvents.Any()) {
			sb.Append($"Worn {wearEvents.Count()} times, last on {wearEvents.Max(x => x.EventDate).ToString("yyyy-MM-dd")}.");
		}

		return sb.ToString();
	}
}