using PerfumeTracker.Server.Features.Outbox;

namespace PerfumeTracker.Server.Features.PerfumeRatings.Services;

public class RatingService(PerfumeTrackerContext context, ISideEffectQueue queue) : IRatingService {
	public async Task<PerfumeRating> AddPerfumeRating(Guid PerfumeId, decimal Rating, string Comment, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var evt = new PerfumeRating {
			PerfumeId = PerfumeId,
			Rating = Rating,
			Comment = Comment,
			RatingDate = DateTime.UtcNow,
		};
		context.PerfumeRatings.Add(evt);
		await context.SaveChangesAsync(cancellationToken);
		await UpdatePerfume(PerfumeId, cancellationToken);
		return evt;
	}

	public async Task<PerfumeRating> DeletePerfumeRating(Guid RatingId, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var rating = await context.PerfumeRatings.FindAsync([RatingId], cancellationToken) ?? throw new NotFoundException("PerfumeRatings", RatingId);
		rating.IsDeleted = true;
		await context.SaveChangesAsync(cancellationToken);
		await UpdatePerfume(rating.PerfumeId, cancellationToken);
		return rating;
	}

	private async Task UpdatePerfume(Guid perfumeId, CancellationToken cancellationToken) {
		var perfume = await context.Perfumes.FindAsync([perfumeId], cancellationToken) ?? throw new NotFoundException("Perfumes", perfumeId);
		perfume.AverageRating = await context.PerfumeRatings
			.Where(r => r.PerfumeId == perfumeId)
			.AverageAsync(r => (decimal?)r.Rating, cancellationToken) ?? 0;
		var message = OutboxMessage.From(new PerfumeUpdatedNotification(perfumeId, perfume.UserId));
		context.OutboxMessages.Add(message);
		await context.SaveChangesAsync(cancellationToken);
		queue.Enqueue(message);
	}
}
