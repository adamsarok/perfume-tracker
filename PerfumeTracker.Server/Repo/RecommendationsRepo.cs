namespace PerfumeTracker.Server.Repo;
public class RecommendationsRepo(PerfumeTrackerContext context) {
	public async Task<List<Recommendation>> GetRecommendations(int dayFilter) {
		return await context
			.Recommendations
			.Where(x => x.CreatedAt >= DateTimeOffset.UtcNow.AddDays(-dayFilter))
			.ToListAsync();
	}
	public async Task<Recommendation?> GetRecommendation(int id) {
		return await context.Recommendations.FindAsync(id);
	}
	public async Task<Recommendation> AddRecommendation(RecommendationUploadDto dto) {
		var r = dto.Adapt<Recommendation>();
		if (r == null) throw new InvalidOperationException("Recommendation mapping failed");
		context.Recommendations.Add(r);
		await context.SaveChangesAsync();
		return r;
	}
}
