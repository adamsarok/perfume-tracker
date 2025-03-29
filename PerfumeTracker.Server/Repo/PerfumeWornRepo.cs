using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Repo;

public class PerfumeWornRepo(PerfumetrackerContext context, SettingsRepo settingsRepo) {

	public async Task<List<PerfumeWornDownloadDto>> GetPerfumesWithWorn(int cursor, int pageSize) {
		return await context
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
	}
	public async Task<List<int>> GetWornPerfumeIDs(int daysFilter) {
		return await context
			.PerfumeWorns
			.Where(x => x.Created_At >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
	}
	public async Task DeletePerfumeWorn(int id) {
		var w = await context.PerfumeWorns.FindAsync(id);
		if (w == null) throw new NotFoundException();
		context.PerfumeWorns.Remove(w);
		await context.SaveChangesAsync();
	}

	public async Task<PerfumeWornDownloadDto> AddPerfumeWorn(PerfumeWornUploadDto dto) {
		var worn = new PerfumeWorn() {
			PerfumeId = dto.PerfumeId,
			Created_At = dto.WornOn
		};
		var settings = await settingsRepo.GetSettingsOrDefault("DEFAULT"); //TODO implement when multi user is needed
		if (settings == null) throw new NotFoundException("Settings", "DEFAULT");
		context.PerfumeWorns.Add(worn);
		var perfume = await context.Perfumes.FindAsync(dto.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", dto.PerfumeId);
		perfume.MlLeft = Math.Max(0, perfume.MlLeft - settings.SprayAmount);
		await context.SaveChangesAsync();
		return worn.Adapt<PerfumeWornDownloadDto>();
	}
}