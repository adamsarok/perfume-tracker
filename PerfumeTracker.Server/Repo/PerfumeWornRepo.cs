namespace PerfumeTracker.Server.Repo;

public class PerfumeEventsRepo(PerfumeTrackerContext context, GetUserProfile getUserProfile, IMediator mediator) {
	public async Task<List<PerfumeWornDownloadDto>> GetPerfumesWithWorn(int cursor, int pageSize) {
		return await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn && (cursor < 1 || x.Id < cursor))
			.OrderByDescending(x => x.Id)
			.Take(pageSize)
			.Select(x => new PerfumeWornDownloadDto(
				x.Id,
				x.CreatedAt,
				x.Perfume.Id,
				x.Perfume.ImageObjectKey,
				"",
				x.Perfume.House,
				x.Perfume.PerfumeName,
				x.Perfume.PerfumeTags.Select(x => x.Tag.Adapt<TagDto>()).ToList()
			))
			.ToListAsync();
	}
	public async Task<List<int>> GetWornPerfumeIDs(int daysFilter) {
		return await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn && x.EventDate >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
	}
	public async Task DeletePerfumeEvent(int id) {
		var w = await context.PerfumeEvents.FindAsync(id);
		if (w == null) throw new NotFoundException();
		w.IsDeleted = true;
		await context.SaveChangesAsync();
	}
}