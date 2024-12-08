using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.DTO;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;

namespace PerfumeTracker.Server.Repo;

public class PerfumeWornRepo
{
    private readonly PerfumetrackerContext _context;

    public PerfumeWornRepo(PerfumetrackerContext context)
    {
        _context = context;
    }
    public async Task<List<PerfumeWornDTO>> GetPerfumesWithWorn(int cursor, int pageSize)
    {
        var raw = await _context
            .PerfumeWorns
            .Where(x => cursor < 1 || x.Id < cursor)
            .OrderByDescending(x => x.Id)
            .Take(pageSize)
            .Select(x => new PerfumeWornDTO(
                x.Id, 
                x.WornOn, 
                x.Perfume.Adapt<PerfumeDTO>(),
                x.Perfume.PerfumeTags.Select(x => x.Tag.Adapt<TagDTO>()).ToList()
            ))
            .ToListAsync();
        return raw;
    }
    public async Task<List<int>> GetWornPerfumeIDs(int daysFilter)
    {
        var r = await _context
            .PerfumeWorns
            .Where(x => x.WornOn >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
            .Select(x => x.PerfumeId)
            .Distinct()
            .ToListAsync();
        return r;
    }
}
