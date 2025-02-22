using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Dto;
using PerfumeTrackerAPI.Models;

namespace PerfumeTracker.Server.Repo;

public class RecommendationsRepo(PerfumetrackerContext context) {
	public async Task<List<Recommendation>> GetRecommendations() {
		return await context
			.Recommendations
			.ToListAsync();
	}
	public async Task<Recommendation> AddRecommendation(RecommendationUploadDto dto) {
		var r = dto.Adapt<Recommendation>();
		if (r == null) throw new InvalidOperationException("Recommendation mapping failed");
		r.Created_At = DateTime.UtcNow;
		context.Recommendations.Add(r);
		await context.SaveChangesAsync();
		return r;
	}
}
