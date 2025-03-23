namespace PerfumeTracker.Server.Repo;
public class PerfumeRepo(PerfumetrackerContext context) {
	public async Task<List<PerfumeWithWornStatsDto>> GetPerfumesWithWorn(string? fulltext = null) {
		var raw = await context
			.Perfumes
			.Include(x => x.PerfumeWorns)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Where(p => string.IsNullOrWhiteSpace(fulltext)
				|| p.FullText.Matches(EF.Functions.PlainToTsQuery($"{fulltext}:*"))
				|| p.PerfumeTags.Any(pt => EF.Functions.ILike(pt.Tag.TagName, fulltext))
				)
			.Select(p => MapToPerfumeWithWornStatsDto(p))
			.AsSplitQuery()
			.AsNoTracking()
			.ToListAsync();
		return raw;
	}
	public async Task<PerfumeWithWornStatsDto?> GetPerfume(int id) {
		var p = await context
			.Perfumes
			.Include(x => x.PerfumeWorns)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Where(p => p.Id == id)
			.Select(p => MapToPerfumeWithWornStatsDto(p))
			.AsSplitQuery()
			.AsNoTracking()
			.FirstOrDefaultAsync();
		if (p == null) throw new NotFoundException();
		return p;
	}
	private static PerfumeWithWornStatsDto MapToPerfumeWithWornStatsDto(Perfume p) {
		return new PerfumeWithWornStatsDto(
				 new PerfumeDto(
					p.Id,
					p.House,
					p.PerfumeName,
					p.Rating,
					p.Notes,
					p.Ml,
					p.ImageObjectKey,
					p.Autumn,
					p.Spring,
					p.Summer,
					p.Winter,
					p.PerfumeTags.Select(tag => new TagDto(tag.Tag.TagName, tag.Tag.Color, tag.Tag.Id)).ToList()
				  ),
				  p.PerfumeWorns.Any() ? p.PerfumeWorns.Count : 0,
				  p.PerfumeWorns.Any() ? p.PerfumeWorns.Max(x => x.Created_At) : null);
	}
	public async Task<PerfumeStatDto> GetPerfumeStats() {
		var raw = await context
			.Perfumes
			.GroupBy(g => 1)
			.Select(x => new PerfumeStatDto(
				x.Sum(s => s.Ml),
				x.Sum(s => s.PerfumeWorns.Count),
				x.Count()))
			.ToListAsync();
		if (raw != null && raw.Count > 0) return raw[0];
		return new PerfumeStatDto(0, 0, 0);
	}
	public async Task<PerfumeDto> AddPerfume(PerfumeDto Dto) {
		var perfume = Dto.Adapt<Perfume>();
		if (perfume == null) throw new InvalidOperationException("Perfume mapping failed");
		perfume.Created_At = DateTime.UtcNow;
		context.Perfumes.Add(perfume);
		await context.SaveChangesAsync();
		foreach (var tag in Dto.Tags) {
			context.PerfumeTags.Add(new PerfumeTag() {
				Created_At = DateTime.UtcNow,
				PerfumeId = perfume.Id,
				TagId = tag.Id,
			});
		}
		await context.SaveChangesAsync();
		return perfume.Adapt<PerfumeDto>();
	}
	public async Task DeletePerfume(int id) {
		var perfume = await context.Perfumes.FindAsync(id);
		if (perfume == null) throw new NotFoundException();
		context.Perfumes.Remove(perfume);
		await context.SaveChangesAsync();
	}
	public async Task<PerfumeDto> UpdatePerfume(int id, PerfumeDto Dto) {
		var perfume = Dto.Adapt<Perfume>();
		if (perfume == null || id != perfume.Id) {
			throw new NotFoundException();
		}
		var find = await context
			.Perfumes
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.FirstOrDefaultAsync(x => x.Id == perfume.Id);
		if (find == null) throw new NotFoundException();

		context.Entry(find).CurrentValues.SetValues(perfume);
		find.Updated_At = DateTime.UtcNow;

		await UpdateTags(Dto, find);

		return find.Adapt<PerfumeDto>();
	}

	public async Task<PerfumeDto> UpdatePerfumeImageGuid(ImageGuidDto Dto) {
		var find = await context.Perfumes.FindAsync(Dto.ParentObjectId);
		if (find == null) throw new NotFoundException();
		find.ImageObjectKey = Dto.ImageObjectKey;
		find.Updated_At = DateTime.UtcNow;
		await context.SaveChangesAsync();
		return find.Adapt<PerfumeDto>();
	}

	private async Task UpdateTags(PerfumeDto Dto, Perfume? find) {
		if (find == null) return;
		var tagsInDB = find.PerfumeTags
			.Select(x => x.Tag)
			.Select(x => x.TagName)
			.ToList();
		foreach (var remove in find.PerfumeTags.Where(x => !Dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
			context.PerfumeTags.Remove(remove);
		}
		if (Dto.Tags != null && Dto.Tags.Any()) {
			foreach (var add in Dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
				context.PerfumeTags.Add(new PerfumeTag() {
					PerfumeId = find.Id,
					TagId = add.Id //TODO: this is not good, tag ID is coming back from client side
				});
			}
		}
		await context.SaveChangesAsync();
	}
}
