using PerfumeTracker.Server.Features.Perfumes.Extensions;

namespace PerfumeTracker.Server.Features.Embedding;

public class UpdateEmbeddings {
	public class PerfumeAddedNotificationHandler(PerfumeTrackerContext context, IEncoder encoder) : INotificationHandler<PerfumeAddedNotification> {
		public async Task Handle(PerfumeAddedNotification notification, CancellationToken cancellationToken) {
			await UpdatePerfumeEmbedding(notification.PerfumeId, context, encoder, cancellationToken);
		}
	}
	public class PerfumeUpdatedNotificationHandler(PerfumeTrackerContext context, IEncoder encoder) : INotificationHandler<PerfumeUpdatedNotification> {
		public async Task Handle(PerfumeUpdatedNotification notification, CancellationToken cancellationToken) {
			await UpdatePerfumeEmbedding(notification.PerfumeId, context, encoder, cancellationToken);
		}
	}
	private static async Task UpdatePerfumeEmbedding(Guid perfumeId, PerfumeTrackerContext context, IEncoder encoder, CancellationToken cancellationToken) {
		var perfume = await context.Perfumes
			.IgnoreQueryFilters()
			.Include(p => p.PerfumeTags).ThenInclude(pt => pt.Tag)
			.Include(p => p.PerfumeRatings)
			.Include(p => p.PerfumeEvents)
			.FirstOrDefaultAsync(p => p.Id == perfumeId, cancellationToken);
		if (perfume == null) return;
		var text = perfume.GetTextForEmbedding();
		var document = await context.PerfumeDocuments.IgnoreQueryFilters().FirstOrDefaultAsync(d => d.Id == perfumeId, cancellationToken);
		if (document == null) {
			var embedding = await encoder.GetEmbeddings(text, cancellationToken);
			document = new PerfumeDocument {
				Id = perfume.Id,
				UserId = perfume.UserId,
				Text = text,
				Embedding = embedding
			};
			context.PerfumeDocuments.Add(document);
			await context.SaveChangesAsync(cancellationToken);
		} else if (text != document.Text) {
			var embedding = await encoder.GetEmbeddings(text, cancellationToken);
			document.Text = text;
			document.Embedding = embedding;
			await context.SaveChangesAsync(cancellationToken);
		}
	}
}