using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Outbox;

namespace PerfumeTracker.Server.Features.PerfumeRandoms;

public record GetRandomPerfumeQuery() : IQuery<GetRandomPerfumeResponse>;
public record GetRandomPerfumeResponse(Guid? PerfumeId, Guid? RandomsId);
public class GetRandomPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/random-perfumes", async (ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GetRandomPerfumeQuery(), cancellationToken);
		})
			.WithTags("PerfumeRandoms")
			.WithName("GetPerfumeSuggestion")
			.RequireAuthorization(Policies.READ);
	}
}
public record class RandomPerfumeAddedNotification(Guid PerfumeId, Guid UserId) : IUserNotification;
public class GetRandomPerfumeHandler(PerfumeTrackerContext context, ISideEffectQueue queue) : IQueryHandler<GetRandomPerfumeQuery, GetRandomPerfumeResponse> {
	public async Task<GetRandomPerfumeResponse> Handle(GetRandomPerfumeQuery request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		var alreadySug = await GetAlreadySuggestedRandomPerfumeIds(settings.DayFilter, cancellationToken);
		var worn = await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn && x.EventDate >= DateTimeOffset.UtcNow.AddDays(-settings.DayFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync(cancellationToken);
		var season = Season;
		var all = await context
			.Perfumes
			.Where(x => x.Ml > 0 && x.PerfumeRatings.Average(x => x.Rating) >= settings.MinimumRating)
			.Where(x => (!x.Winter && !x.Spring && !x.Summer && !x.Autumn) ||
				(season == Seasons.Autumn && x.Autumn) ||
				(season == Seasons.Winter && x.Winter) ||
				(season == Seasons.Spring && x.Spring) ||
				(season == Seasons.Summer && x.Summer)
			)
			.Select(x => x.Id)
			.ToListAsync(cancellationToken);
		if (all.Count == 0) return new GetRandomPerfumeResponse(null, null);
		var filtered = all.Except(alreadySug).Except(worn);
		if (!filtered.Any()) filtered = all.Except(worn);
		if (!filtered.Any()) filtered = all;
		int index = _random.Next(filtered.Count());
		var result = filtered.ToArray()[index];
		var randoms = await AddRandomPerfume(result, userId, cancellationToken);
		return new GetRandomPerfumeResponse(result, randoms.Id);
	}
	private async Task<Models.PerfumeRandoms> AddRandomPerfume(Guid perfumeId, Guid userId, CancellationToken cancellationToken) {
		var p = await context.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId, cancellationToken);
		if (p == null) throw new NotFoundException();
		var s = new Models.PerfumeRandoms() {
			PerfumeId = perfumeId,
		};
		var message = OutboxMessage.From(new RandomPerfumeAddedNotification(perfumeId, userId));
		context.OutboxMessages.Add(message);
		context.PerfumeRandoms.Add(s);
		await context.SaveChangesAsync(cancellationToken);
		queue.Enqueue(message);
		return s;
	}
	private async Task<List<Guid>> GetAlreadySuggestedRandomPerfumeIds(int daysFilter, CancellationToken cancellationToken) {
		return await context
			.PerfumeRandoms
			.Where(x => x.CreatedAt >= DateTime.UtcNow.AddDays(-daysFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync(cancellationToken);
	}
	enum Seasons { Winter, Spring, Summer, Autumn };
	private static readonly Random _random = new();
	private static Seasons Season => DateTime.Now.Month switch {
		1 or 2 or 12 => Seasons.Winter,
		3 or 4 or 5 => Seasons.Spring,
		6 or 7 or 8 => Seasons.Summer,
		9 or 10 or 11 => Seasons.Autumn,
		_ => throw new NotImplementedException(),
	};

}