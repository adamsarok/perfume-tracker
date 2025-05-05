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
				x.WornOn,
				x.Perfume.Adapt<PerfumeDto>(),
				x.Perfume.PerfumeTags.Select(x => x.Tag.Adapt<TagDto>()).ToList()
			))
			.ToListAsync();
	}
	public async Task<List<int>> GetWornPerfumeIDs(int daysFilter) {
		return await context
			.PerfumeWorns
			.Where(x => x.WornOn >= DateTimeOffset.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
	}
	public async Task DeletePerfumeWorn(int id) {
		var w = await context.PerfumeWorns.FindAsync(id);
		if (w == null) throw new NotFoundException();
		context.PerfumeWorns.Remove(w);
		var settings = await settingsRepo.GetSettingsOrDefault("DEFAULT"); //TODO implement when multi user is needed
		var perfume = await context.Perfumes.FindAsync(w.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", w.PerfumeId);
		perfume.MlLeft = Math.Min(perfume.Ml, perfume.MlLeft + settings.SprayAmountForBottleSize(perfume.Ml));
		await context.SaveChangesAsync();
	}

	public async Task<PerfumeWornDownloadDto> AddPerfumeWorn(PerfumeWornUploadDto dto) {
		var worn = dto.Adapt<PerfumeWorn>();
		var settings = await settingsRepo.GetSettingsOrDefault("DEFAULT"); //TODO implement when multi user is needed
		context.PerfumeWorns.Add(worn);
		var perfume = await context.Perfumes.FindAsync(dto.PerfumeId);
		if (perfume == null) throw new NotFoundException("Perfume", dto.PerfumeId);
		perfume.MlLeft = Math.Max(0, perfume.MlLeft - settings.SprayAmountForBottleSize(perfume.Ml));
		await context.SaveChangesAsync();
		return worn.Adapt<PerfumeWornDownloadDto>();
	}
}