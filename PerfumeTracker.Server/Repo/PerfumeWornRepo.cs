using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Dto;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.Repo;

public class PerfumeWornRepo(PerfumetrackerContext context) {

    public async Task<List<PerfumeWornDownloadDto>> GetPerfumesWithWorn(int cursor, int pageSize) {
        var raw = await context
            .PerfumeWorns
            .Where(x => cursor < 1 || x.Id < cursor)
            .OrderByDescending(x => x.Id)
            .Take(pageSize)
            .Select(x => new PerfumeWornDownloadDto(
                x.Id,
                x.Created_At,
                x.Perfume.Adapt<PerfumeDto>(),
                x.Perfume.PerfumeTags.Select(x => x.Tag.Adapt<TagDto>()).ToList()
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
    public record PerfumeWornResult(ResultTypes ResultType, PerfumeWornDownloadDto? worn = null, string? ErrorMsg = null);
    public async Task<PerfumeWornResult> DeletePerfumeWorn(int id) {
        var w = await context.PerfumeWorns.FindAsync(id);
        if (w == null) return new PerfumeWornResult(ResultTypes.NotFound);
        context.PerfumeWorns.Remove(w);
        await context.SaveChangesAsync();
        return new PerfumeWornResult(ResultTypes.Ok);
    }

    public async Task<PerfumeWornResult> AddPerfumeWorn(PerfumeWornUploadDto dto) {
        try {
            if (await context.PerfumeWorns.AnyAsync(x =>
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
            return new PerfumeWornResult(ResultTypes.Ok, w.Adapt<PerfumeWornDownloadDto>());
        }
        catch (Exception ex) {
            return new PerfumeWornResult(ResultTypes.BadRequest, null, ex.Message);
        }
    }

}