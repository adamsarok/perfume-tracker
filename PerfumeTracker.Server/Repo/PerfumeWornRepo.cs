using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Migrations;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Repo;

public class PerfumeEventsRepo(PerfumeTrackerContext context, GetUserProfile getUserProfile, IMediator mediator) {
	public record class PerfumeWornAddedEvent(PerfumeWorn PerfumeWorn) : INotification;
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

	public async Task<PerfumeWornDownloadDto> AddPerfumeEvent(PerfumeEventUploadDto dto) {
		var evt = dto.Adapt<PerfumeWorn>();
		var settings = await getUserProfile.HandleAsync();
		context.PerfumeEvents.Add(evt);
		var perfume = await context.Perfumes.FindAsync(evt.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", evt.PerfumeId);
		if (evt.AmountMl == 0 && evt.Type == PerfumeWorn.PerfumeEventType.Worn) {
			evt.AmountMl = -settings.SprayAmountForBottleSize(perfume.Ml);
		}
		await context.SaveChangesAsync();
		if (evt.Type == PerfumeWorn.PerfumeEventType.Worn) {
			await mediator.Publish(new PerfumeWornAddedEvent(evt));
		}
		return evt.Adapt<PerfumeWornDownloadDto>();
	}
}