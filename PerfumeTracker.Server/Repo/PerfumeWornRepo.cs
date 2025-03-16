using System;
using Mapster;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Dto;
using PerfumeTracker.Server.Exceptions;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;

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
		var t = await context.PerfumeWorns.ToListAsync();
		var r = await context
			.PerfumeWorns
			.Where(x => x.Created_At >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
		return r;
	}
	public async Task DeletePerfumeWorn(int id) {
		var w = await context.PerfumeWorns.FindAsync(id);
		if (w == null) throw new NotFoundException();
		context.PerfumeWorns.Remove(w);
		await context.SaveChangesAsync();
	}

	public async Task<PerfumeWornDownloadDto> AddPerfumeWorn(PerfumeWornUploadDto dto) {
		// if (await context.PerfumeWorns.AnyAsync(x =>
		// 	x.PerfumeId == dto.PerfumeId &&
		// 	x.Created_At.Date == dto.WornOn.Date
		// )) {
		// 	throw new InvalidOperationException("You have already worn this perfume on this day");
		// } //TODO: maybe reenable with setting? there should be a possibility to respray a perfume
		var w = new PerfumeWorn() {
			PerfumeId = dto.PerfumeId,
			Created_At = dto.WornOn
		};
		context.PerfumeWorns.Add(w);
		await context.SaveChangesAsync();
		return w.Adapt<PerfumeWornDownloadDto>();
	}
}