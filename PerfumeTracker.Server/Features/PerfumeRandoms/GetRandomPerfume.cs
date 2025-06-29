

using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.UserProfiles;

namespace PerfumeTracker.Server.Features.PerfumeRandoms;
public record GetRandomPerfumeQuery() : IQuery<GetRandomPerfumeResponse>;
public record GetRandomPerfumeResponse(Guid? PerfumeId);
public class GetRandomPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/random-perfumes", async (ISender sender) => {
			var result = await sender.Send(new GetRandomPerfumeQuery());
			return result.PerfumeId;
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
		var alreadySug = await GetAlreadySuggestedRandomPerfumeIds(settings.DayFilter);
		var worn = await context
			.PerfumeEvents
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn && x.EventDate >= DateTimeOffset.UtcNow.AddDays(-settings.DayFilter))
			.Select(x => x.PerfumeId)
			.Distinct()
			.ToListAsync();
		var season = Season;
		var all = await context
			.Perfumes
			.Where(x => x.Ml > 0 && x.Rating >= settings.MinimumRating)
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
		if (!alreadySug.Contains(result)) await AddRandomPerfume(result, userId);
		return new GetRandomPerfumeResponse(result);
	}
	private async Task AddRandomPerfume(Guid perfumeId, Guid userId) {
		var p = await context.Perfumes.FirstOrDefaultAsync(x => x.Id == perfumeId);
		if (p == null) throw new NotFoundException();
		var s = new Models.PerfumeRandoms() {
			PerfumeId = perfumeId,
		};
		var message = OutboxMessage.From(new RandomPerfumeAddedNotification(perfumeId, userId));
		context.OutboxMessages.Add(message);
		context.PerfumeRandoms.Add(s);
		await context.SaveChangesAsync();
		queue.Enqueue(message);
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