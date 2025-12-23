using System.Text;

namespace PerfumeTracker.Server.Services.Embedding;

public class EmbeddingRetryService(IServiceProvider sp, ILogger<EmbeddingRetryService> logger, IEncoder encoder) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				await Backfill(stoppingToken);
				await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
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
	private async Task Backfill(CancellationToken cancellationToken) {
		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var test = await context.Perfumes.CountAsync(cancellationToken);

		// Find perfumes that need embeddings
		var perfumesNeedingEmbedding = await context.Perfumes
			.IgnoreQueryFilters()
			.Where(p => !context.PerfumeDocuments.Any(d => d.Id == p.Id) && !p.IsDeleted)
			.Take(100)
			.Include(p => p.PerfumeTags).ThenInclude(pt => pt.Tag)
			.Include(p => p.PerfumeRatings)
			.Include(p => p.PerfumeEvents)
			.ToListAsync(cancellationToken);

		if (perfumesNeedingEmbedding.Count == 0) return;

		logger.LogInformation("Processing {Count} perfumes for embeddings", perfumesNeedingEmbedding.Count);

		foreach (var perfume in perfumesNeedingEmbedding) {
			try {
				var text = BuildText(perfume);
				var embedding = await encoder.GetEmbeddings(text);

				context.PerfumeDocuments.Add(new PerfumeDocument {
					Id = perfume.Id,
					Text = text,
					Embedding = embedding,
					UserId = perfume.UserId
				});

				await context.SaveChangesAsync(cancellationToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Failed to generate embedding for perfume {PerfumeId}", perfume.Id);
			}
		}
	}

	private string BuildText(Perfume perfume) {
		var sb = new StringBuilder(); // only add fields which are not available in perfume. Eg House can be searched in Perfume - sentiment based on all tags & comments can not

		if (perfume.PerfumeTags.Any()) {
			sb.Append($"Perfume has {string.Join(", ", perfume.PerfumeTags.Select(pt => pt.Tag.TagName))} notes. ");
		}

		if (perfume.PerfumeRatings.Any()) {
			sb.Append($"User rated perfume {perfume.PerfumeRatings.Average(x => x.Rating).ToString("0.#")} points. ");
			sb.Append($"User thinks perfume ");
			var comments = perfume.PerfumeRatings
				.Where(r => !string.IsNullOrWhiteSpace(r.Comment))
				.OrderByDescending(r => r.UpdatedAt)
				.Take(10)
				.Select(r => r.Comment);
			if (comments.Any()) {
				sb.Append($"{string.Join(". ", comments)} ");
			}
		}

		return sb.ToString();
	}
}