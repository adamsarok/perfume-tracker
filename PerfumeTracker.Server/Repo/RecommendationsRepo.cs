using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.DTO;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.Repo;

public class RecommendationsRepo {
    private readonly PerfumetrackerContext _context;

    public RecommendationsRepo(PerfumetrackerContext context) {
        _context = context;
    }

    public async Task<List<Recommendation>> GetRecommendations() {
        return await _context
            .Recommendations
            .ToListAsync();
    }

    public record RecommendationsResult(ResultTypes ResultType, Recommendation? Recommendation = null, string ErrorMsg = null);
    public async Task<RecommendationsResult> AddRecommendation(RecommendationUploadDTO dto) {
        try {
            var r = dto.Adapt<Recommendation>();
            if (r == null) return new RecommendationsResult(ResultTypes.BadRequest);
            r.Created_At = DateTime.UtcNow;
            _context.Recommendations.Add(r);
            await _context.SaveChangesAsync();
            return new RecommendationsResult(ResultTypes.Ok, r);
        }
        catch (Exception ex) {
            return new RecommendationsResult(ResultTypes.BadRequest, null, ex.Message);
        }
    }
}
