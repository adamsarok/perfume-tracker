namespace PerfumeTracker.Server.Repo;

public class RandomPerfumeRepo(PerfumeTrackerContext context, PerfumeEventsRepo perfumeWornRepo) {
	public record class RandomPerfumeAddedEvent() : INotification;
	public async Task AddRandomPerfume(Guid perfumeId) {
		var p = await context.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId);
		if (p == null) throw new NotFoundException();
		var s = new PerfumeRandoms() {
			PerfumeId = perfumeId,
		};
		context.OutboxMessages.Add(OutboxMessage.From(new RandomPerfumeAddedEvent()));
		context.PerfumeRandoms.Add(s);
		await context.SaveChangesAsync();
	}
	public async Task<List<Guid>> GetAlreadySuggestedRandomPerfumeIds(int daysFilter) {
		return await context
			.PerfumeRandoms
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
	public async Task<Guid?> GetRandomPerfume(int daysFilter, double minimumRating) {
		var alreadySug = await GetAlreadySuggestedRandomPerfumeIds(daysFilter);
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
		if (all.Count == 0) return null;
		var filtered = all.Except(alreadySug).Except(worn);
		if (!filtered.Any()) filtered = all.Except(worn);
		if (!filtered.Any()) filtered = all;
		int index = _random.Next(filtered.Count());
		var result = filtered.ToArray()[index];
		if (!alreadySug.Contains(result)) await AddRandomPerfume(result);
		return result;
	}
	public record class RandomsAcceptedNotification() : INotification;
	public async Task AcceptRandomPerfume(int randomsId) {
		var perfumeRandom = await context.PerfumeRandoms.FindAsync(randomsId);
		perfumeRandom.IsAccepted = true;
		context.OutboxMessages.Add(OutboxMessage.From(new RandomsAcceptedNotification()));
		await context.SaveChangesAsync();
	}
}
