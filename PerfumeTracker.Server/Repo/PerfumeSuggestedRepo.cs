using System;
using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.Repo;

public class PerfumeSuggestedRepo(PerfumetrackerContext context, PerfumeWornRepo perfumeWornRepo) {
    public record PerfumeSuggestedResult(ResultTypes ResultType, string ErrorMsg = null);
    public async Task<PerfumeSuggestedResult> AddSuggestedPerfume(int perfumeId) {
        try {
            var p = await context.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId);
            if (p == null) return new PerfumeSuggestedResult(ResultTypes.BadRequest, "Perfume not found");
            var s = new PerfumeSuggested() {
                PerfumeId = perfumeId,
                Created_At = DateTime.UtcNow,
            }; 
            context.PerfumeSuggesteds.Add(s);
            await context.SaveChangesAsync();
            return new PerfumeSuggestedResult(ResultTypes.Ok);
        }
        catch (Exception ex) {
            return new PerfumeSuggestedResult(ResultTypes.BadRequest, ex.Message);
        } //TODO: global error handling is better than this half measure, replace
    }
    public async Task<List<int>> GetAlreadySuggestedPerfumeIds(int daysFilter) {
        var r = await context
            .PerfumeSuggesteds
            .Where(x => x.Created_At >= DateTime.UtcNow.AddDays(-daysFilter))
            .Select(x => x.PerfumeId)
            .Distinct()
            .ToListAsync();
        return r;
    }
    public async Task<int> GetPerfumeSuggestion(int daysFilter) {
        var alreadySug = await GetAlreadySuggestedPerfumeIds(daysFilter);
        var worn = await perfumeWornRepo.GetWornPerfumeIDs(daysFilter);
        var result = await context
            .Perfumes
            .Where(x => !alreadySug.Contains(x.Id) && !worn.Contains(x.Id))
            .Where(x => x.Ml > 0 && x.Rating >= 8)
            .Select(x => x.Id)
            .FirstOrDefaultAsync();
        await AddSuggestedPerfume(result);
        return result;
    }
}
