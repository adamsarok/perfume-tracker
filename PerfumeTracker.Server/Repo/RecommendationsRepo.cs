using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Dto;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.Repo;

public class RecommendationsRepo(PerfumetrackerContext context) {
    public async Task<List<Recommendation>> GetRecommendations() {
        return await context
            .Recommendations
            .ToListAsync();
    }

    public record RecommendationsResult(ResultTypes ResultType, Recommendation? Recommendation = null, string ErrorMsg = null);
    public async Task<RecommendationsResult> AddRecommendation(RecommendationUploadDto dto) {
        try {
            var r = dto.Adapt<Recommendation>();
            if (r == null) return new RecommendationsResult(ResultTypes.BadRequest);
            r.Created_At = DateTime.UtcNow;
            context.Recommendations.Add(r);
            await context.SaveChangesAsync();
            return new RecommendationsResult(ResultTypes.Ok, r);
        }
        catch (Exception ex) {
            return new RecommendationsResult(ResultTypes.BadRequest, null, ex.Message);
        }
    }
}
