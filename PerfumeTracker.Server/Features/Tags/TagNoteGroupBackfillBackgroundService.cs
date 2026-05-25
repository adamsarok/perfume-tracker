using PerfumeTracker.Server.Features.Tags.Services;

namespace PerfumeTracker.Server.Features.Tags;

using PerfumeTracker.Server.Startup;
using System.Diagnostics;

public class TagNoteGroupBackfillBackgroundService(
	IServiceProvider sp,
	ILogger<TagNoteGroupBackfillBackgroundService> logger) : BackgroundService {

	private const int BATCH_SIZE = 25;

	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

		while (!stoppingToken.IsCancellationRequested) {
			try {
				bool hasUpdates = await BackfillBatch(stoppingToken);
				await Task.Delay(hasUpdates ? TimeSpan.FromSeconds(30) : TimeSpan.FromHours(1), stoppingToken);
			} catch (Exception ex) {
				logger.LogError(ex, "Error processing tag note group backfill");
				await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
			}
		}
	}

	private async Task<bool> BackfillBatch(CancellationToken cancellationToken) {
		using var activity = Diagnostics.ActivitySource.StartActivity("tag_note_group.backfill", ActivityKind.Internal);
		activity?.SetTag("job.name", nameof(TagNoteGroupBackfillBackgroundService));

		using var scope = sp.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var identifier = scope.ServiceProvider.GetRequiredService<ITagNoteGroupIdentifier>();

		var tags = await context.Tags
			.IgnoreQueryFilters()
			.Where(t => !t.IsDeleted && string.IsNullOrEmpty(t.NoteGroup))
			.OrderBy(t => t.Id)
			.Take(BATCH_SIZE)
			.ToListAsync(cancellationToken);

		if (tags.Count == 0) {
			activity?.SetTag("tag.count", 0);
			return false;
		}

		activity?.SetTag("tag.count", tags.Count);
		logger.LogInformation("Processing {Count} tags for note group backfill", tags.Count);

		var tagsByUser = tags.GroupBy(t => t.UserId);
		activity?.SetTag("user.count", tagsByUser.Count());
		var updatedCount = 0;

		foreach (var userTags in tagsByUser) {
			using var userActivity = Diagnostics.ActivitySource.StartActivity("tag_note_group.backfill.user", ActivityKind.Internal);
			userActivity?.SetTag("tag.count", userTags.Count());

			var identified = await identifier.GetIdentifiedTagNoteGroupsAsync(
				userTags.Select(t => t.TagName).ToList(),
				userTags.Key,
				cancellationToken);

			foreach (var result in identified.Tags) {
				if (string.IsNullOrWhiteSpace(result.NoteGroup)) continue;

				var tag = userTags.FirstOrDefault(t => t.TagName.Equals(result.TagName, StringComparison.OrdinalIgnoreCase));
				if (tag == null || !string.IsNullOrEmpty(tag.NoteGroup)) continue;

				tag.NoteGroup = result.NoteGroup;
				updatedCount++;
			}
		}

		await context.SaveChangesAsync(cancellationToken);
		activity?.SetTag("tag.updated_count", updatedCount);
		logger.LogInformation("Successfully updated note groups for {Count} tags", updatedCount);
		return updatedCount > 0;
	}
}
