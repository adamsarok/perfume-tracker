namespace PerfumeTracker.Server.Features.Perfumes.Services;

public static class RecommendationQueries {
	public static IQueryable<Perfume> Eligible(PerfumeTrackerContext context, decimal minimumRating, List<Guid> excludedIds) =>
		context.Perfumes.Where(p => p.MlLeft > 0 && p.LatestRating >= minimumRating && !excludedIds.Contains(p.Id));
}
