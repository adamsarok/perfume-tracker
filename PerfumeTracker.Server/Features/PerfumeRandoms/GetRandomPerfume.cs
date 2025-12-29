using PerfumeTracker.Server.Features.Perfumes.Services;
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
public class GetRandomPerfumeHandler(PerfumeTrackerContext context, ISideEffectQueue queue, IPerfumeRecommender perfumeRecommender) : IQueryHandler<GetRandomPerfumeQuery, GetRandomPerfumeResponse> {
	public async Task<GetRandomPerfumeResponse> Handle(GetRandomPerfumeQuery request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		var results = await perfumeRecommender.GetRecommendationsForStrategy(RecommendationStrategy.Random, 1, cancellationToken);
		if (!results.Any()) return new GetRandomPerfumeResponse(null, null);
		var result = results.First();
		var randoms = await AddRandomPerfume(result.Perfume.Perfume.Id, userId, cancellationToken);
		return new GetRandomPerfumeResponse(result.Perfume.Perfume.Id, randoms.Id);
	}

	/// <summary>
	/// This method ensures when user is re-rolling using the single random perfume button, they dont get the same perfume again.
	/// </summary>
	/// <param name="perfumeId"></param>
	/// <param name="userId"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="NotFoundException"></exception>
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
	private static readonly Random _random = new();
}