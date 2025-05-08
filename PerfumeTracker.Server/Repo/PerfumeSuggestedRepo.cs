namespace PerfumeTracker.Server.Repo;

public class PerfumeSuggestedRepo(PerfumetrackerContext context, PerfumeEventsRepo perfumeWornRepo) {
	public async Task AddSuggestedPerfume(int perfumeId) {
		var p = await context.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId);
		if (p == null) throw new NotFoundException();
		var s = new PerfumeSuggested() {
			PerfumeId = perfumeId,
		};
		context.PerfumeSuggesteds.Add(s);
		await context.SaveChangesAsync();
	}
	public async Task<List<int>> GetAlreadySuggestedPerfumeIds(int daysFilter) {
		return await context
			.PerfumeSuggesteds
			.Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
	}
	enum Seasons { Winter, Spring, Summer, Autumn };
	private static readonly Random _random = new();
	private static Seasons Season => DateTime.Now.Month switch {
		1 or 2 or 12 => Seasons.Winter,
		3 or 4 or 5 => Seasons.Spring,
		6 or 7 or 8 => Seasons.Summer,
		9 or 10 or 11 => Seasons.Autumn,
	};
	public async Task<int> GetPerfumeSuggestion(int daysFilter, double minimumRating) {
		var alreadySug = await GetAlreadySuggestedPerfumeIds(daysFilter);
		var worn = await perfumeWornRepo.GetWornPerfumeIDs(daysFilter);
		var season = Season;
		var all = await context
			.Perfumes
			.Where(x => x.Ml > 0 && x.Rating >= minimumRating)
			.Where(x => (!x.Winter && !x.Spring && !x.Summer && !x.Autumn) ||
				(season == Seasons.Autumn && x.Autumn) ||
				(season == Seasons.Winter && x.Winter) ||
				(season == Seasons.Spring && x.Spring) ||
				(season == Seasons.Summer && x.Summer)
			)
			.Select(x => x.Id)
			.ToListAsync();
		if (all.Count == 0) return 0;
		var filtered = all.Except(alreadySug).Except(worn);
		if (!filtered.Any()) filtered = all.Except(worn);
		if (!filtered.Any()) filtered = all;
		int index = _random.Next(filtered.Count());
		var result = filtered.ToArray()[index];
		if (!alreadySug.Contains(result)) await AddSuggestedPerfume(result);
		return result;
	}
}
