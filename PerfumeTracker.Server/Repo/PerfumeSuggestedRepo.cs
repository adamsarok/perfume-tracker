using System;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.Repo;

public class PerfumeSuggestedRepo {
    private readonly PerfumetrackerContext _context;

    public PerfumeSuggestedRepo(PerfumetrackerContext context) {
        _context = context;
    }
    public record PerfumeSuggestedResult(ResultTypes ResultType, string ErrorMsg = null);
    public async Task<PerfumeSuggestedResult> AddSuggestedPerfume(int perfumeId) {
        try {
            var p = await _context.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId);
            if (p == null) return new PerfumeSuggestedResult(ResultTypes.BadRequest, "Perfume not found");
            var s = new PerfumeSuggested() {
                PerfumeId = perfumeId,
                Created_At = DateTime.UtcNow,
            }; 
            //TODO: there is a mix of utc and local. prisma created timestamp(3) and ef migrations created timestamptz
            _context.PerfumeSuggesteds.Add(s);
            await _context.SaveChangesAsync();
            return new PerfumeSuggestedResult(ResultTypes.Ok);
        }
        catch (Exception ex) {
            return new PerfumeSuggestedResult(ResultTypes.BadRequest, ex.Message);
        } //TODO: global error handling is better than this half measure, replace
    }
    public async Task<List<int>> GetAlreadySuggestedPerfumeIds(int daysFilter) {
        var r = await _context
            .PerfumeSuggesteds
            .Where(x => x.Created_At >= DateTime.UtcNow.AddDays(-daysFilter))
            .Select(x => x.PerfumeId)
            .Distinct()
            .ToListAsync();
        return r;
    }
}
