using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.DTO;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.Repo;

public class PerfumeWornRepo {
    private readonly PerfumetrackerContext _context;

    public PerfumeWornRepo(PerfumetrackerContext context) {
        _context = context;
    }
    public async Task<List<PerfumeWornDownloadDTO>> GetPerfumesWithWorn(int cursor, int pageSize) {
        var raw = await _context
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
        var r = await _context
            .PerfumeWorns
            .Where(x => x.Created_At >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
            .Select(x => x.PerfumeId)
            .Distinct()
            .ToListAsync();
        return r;
    }
    public record PerfumeWornResult(ResultTypes ResultType, PerfumeWornDownloadDTO? worn = null, string? ErrorMsg = null);
    public async Task<PerfumeWornResult> DeletePerfumeWorn(int id) {
        var w = await _context.PerfumeWorns.FindAsync(id);
        if (w == null) return new PerfumeWornResult(ResultTypes.NotFound);
        _context.PerfumeWorns.Remove(w);
        await _context.SaveChangesAsync();
        return new PerfumeWornResult(ResultTypes.Ok);
    }

    public async Task<PerfumeWornResult> AddPerfumeWorn(PerfumeWornUploadDTO dto) {
        try {
            if (_context.PerfumeWorns.Any(x =>
                x.PerfumeId == dto.perfumeId &&
                x.Created_At.Date == dto.wornOn.Date
            )) {
                return new PerfumeWornResult(ResultTypes.BadRequest, null, "You have already worn this perfume on this day");
            }
            var w = new PerfumeWorn() {
                PerfumeId = dto.perfumeId,
                Created_At = dto.wornOn
            };
            _context.PerfumeWorns.Add(w);
            await _context.SaveChangesAsync();
            return new PerfumeWornResult(ResultTypes.Ok, w.Adapt<PerfumeWornDownloadDTO>());
        }
        catch (Exception ex) {
            return new PerfumeWornResult(ResultTypes.BadRequest, null, ex.Message);
        }
    }

}