

using PerfumeTracker.Server.Features.PerfumeEvents;

namespace PerfumeTracker.Server.Features.PerfumeRandoms;
public record GetRandomPerfumeQuery(int DaysFilter, double MinimumRating) : IQuery<GetRandomPerfumeResponse>;
public record GetRandomPerfumeResponse(Guid? PerfumeId);
public class GetRandomPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		throw new NotImplementedException();
	}
}
public record class RandomPerfumeAddedEvent(Guid PerfumeId) : INotification;
public class GetRandomPerfumeHandler(PerfumeTrackerContext context, ISender sender) : IQueryHandler<GetRandomPerfumeQuery, GetRandomPerfumeResponse> {
	public async Task<GetRandomPerfumeResponse> Handle(GetRandomPerfumeQuery request, CancellationToken cancellationToken) {
		var alreadySug = await GetAlreadySuggestedRandomPerfumeIds(request.DaysFilter);
		var worn = await sender.Send(new GetWornPerfumeIdsQuery(request.DaysFilter));
		var season = Season;
		var all = await context
			.Perfumes
			.Where(x => x.Ml > 0 && x.Rating >= request.MinimumRating)
			.Where(x => (!x.Winter && !x.Spring && !x.Summer && !x.Autumn) ||
				(season == Seasons.Autumn && x.Autumn) ||
				(season == Seasons.Winter && x.Winter) ||
				(season == Seasons.Spring && x.Spring) ||
				(season == Seasons.Summer && x.Summer)
			)
			.Select(x => x.Id)
			.ToListAsync();
		if (all.Count == 0) return new GetRandomPerfumeResponse(null);
		var filtered = all.Except(alreadySug).Except(worn);
		if (!filtered.Any()) filtered = all.Except(worn);
		if (!filtered.Any()) filtered = all;
		int index = _random.Next(filtered.Count());
		var result = filtered.ToArray()[index];
		if (!alreadySug.Contains(result)) await AddRandomPerfume(result);
		return new GetRandomPerfumeResponse(result);
	}
	private async Task AddRandomPerfume(Guid perfumeId) {
		var p = await context.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId);
		if (p == null) throw new NotFoundException();
		var s = new Models.PerfumeRandoms() {
			PerfumeId = perfumeId,
		};
		context.OutboxMessages.Add(OutboxMessage.From(new RandomPerfumeAddedEvent(perfumeId)));
		context.PerfumeRandoms.Add(s);
		await context.SaveChangesAsync();
	}
	private async Task<List<Guid>> GetAlreadySuggestedRandomPerfumeIds(int daysFilter) {
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
	
}