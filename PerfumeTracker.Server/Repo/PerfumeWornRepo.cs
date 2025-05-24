namespace PerfumeTracker.Server.Repo;

public class PerfumeEventsRepo(PerfumeTrackerContext context, GetUserProfile getUserProfile, IMediator mediator) {
	public async Task<List<PerfumeWornDownloadDto>> GetPerfumesWithWorn(DateTime? cursor, int pageSize) {
		return await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn && (!cursor.HasValue || x.CreatedAt < cursor.Value))
			.OrderByDescending(x => x.CreatedAt)
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
	public async Task<List<Guid>> GetWornPerfumeIDs(int daysFilter) {
		return await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn && x.EventDate >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
	}
	public async Task DeletePerfumeEvent(Guid id) {
		var w = await context.PerfumeEvents.FindAsync(id);
		if (w == null) throw new NotFoundException();
		w.IsDeleted = true;
		await context.SaveChangesAsync();
	}
}