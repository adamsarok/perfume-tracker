namespace PerfumeTracker.Server.Features.PerfumeRatings.Services;

public class RatingService(PerfumeTrackerContext context) : IRatingService {
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
		await context.SaveChangesAsync(cancellationToken);
		return evt;
	}

	public async Task<PerfumeRating> DeletePerfumeRating(Guid RatingId, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var rating = await context.PerfumeRatings.FindAsync([RatingId], cancellationToken) ?? throw new NotFoundException("PerfumeRatings", RatingId);
		rating.IsDeleted = true;
		await context.SaveChangesAsync(cancellationToken);
		await UpdatePerfume(rating.PerfumeId, cancellationToken);
		await context.SaveChangesAsync(cancellationToken);
		return rating;
	}

	private async Task UpdatePerfume(Guid perfumeId, CancellationToken cancellationToken) {
		var perfume = await context.Perfumes.FindAsync([perfumeId], cancellationToken) ?? throw new NotFoundException("Perfumes", perfumeId);
		perfume.AverageRating = await context.PerfumeRatings
			.Where(r => r.PerfumeId == perfumeId)
			.AverageAsync(r => (decimal?)r.Rating, cancellationToken) ?? 0;
	}
}
