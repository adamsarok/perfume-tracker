using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.DTO;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.Repo;

public class PerfumeWornRepo(PerfumetrackerContext context) {

    public async Task<List<PerfumeWornDownloadDTO>> GetPerfumesWithWorn(int cursor, int pageSize) {
        var raw = await context
            .PerfumeWorns
            .Where(x => cursor < 1 || x.Id < cursor)
            .OrderByDescending(x => x.Id)
            .Take(pageSize)
            .Select(x => new PerfumeWornDownloadDTO(
                x.Id,
                x.Created_At,
                x.Perfume.Adapt<PerfumeDTO>(),
                x.Perfume.PerfumeTags.Select(x => x.Tag.Adapt<TagDTO>()).ToList()
            ))
            .ToListAsync();
        return raw;
    }
    public async Task<List<int>> GetWornPerfumeIDs(int daysFilter) {
        var r = await context
            .PerfumeWorns
            .Where(x => x.Created_At >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
            .Select(x => x.PerfumeId)
            .Distinct()
            .ToListAsync();
        return r;
    }
    public record PerfumeWornResult(ResultTypes ResultType, PerfumeWornDownloadDTO? worn = null, string? ErrorMsg = null);
    public async Task<PerfumeWornResult> DeletePerfumeWorn(int id) {
        var w = await context.PerfumeWorns.FindAsync(id);
        if (w == null) return new PerfumeWornResult(ResultTypes.NotFound);
        context.PerfumeWorns.Remove(w);
        await context.SaveChangesAsync();
        return new PerfumeWornResult(ResultTypes.Ok);
    }

    public async Task<PerfumeWornResult> AddPerfumeWorn(PerfumeWornUploadDTO dto) {
        try {
            if (context.PerfumeWorns.Any(x =>
                x.PerfumeId == dto.PerfumeId &&
                x.Created_At.Date == dto.WornOn.Date
            )) {
                return new PerfumeWornResult(ResultTypes.BadRequest, null, "You have already worn this perfume on this day");
            }
            var w = new PerfumeWorn() {
                PerfumeId = dto.PerfumeId,
                Created_At = dto.WornOn
            };
            context.PerfumeWorns.Add(w);
            await context.SaveChangesAsync();
            return new PerfumeWornResult(ResultTypes.Ok, w.Adapt<PerfumeWornDownloadDTO>());
        }
        catch (Exception ex) {
            return new PerfumeWornResult(ResultTypes.BadRequest, null, ex.Message);
        }
    }

}