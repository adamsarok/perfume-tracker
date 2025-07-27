using PerfumeTracker.Server.Features.Common;

namespace PerfumeTracker.Server.Features.Perfumes;
public record GetNextPerfumeIdQuery(Guid Id) : IQuery<Guid>;
public record GetPreviousPerfumeIdQuery(Guid Id) : IQuery<Guid>;
public class GetAdjacentPerfumeIdEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/{id}/next", async (Guid id, ISender sender) => {
			return await sender.Send(new GetNextPerfumeIdQuery(id));
		})
			.WithTags("Perfumes")
			.WithName("GetNextPerfume")
			.RequireAuthorization(Policies.READ);
		app.MapGet("/api/perfumes/{id}/previous", async (Guid id, ISender sender) => {
			return await sender.Send(new GetPreviousPerfumeIdQuery(id));
		})
			.WithTags("Perfumes")
			.WithName("GetPreviousPerfume")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetNextPerfumeHandler(PerfumeTrackerContext context)
		: IQueryHandler<GetNextPerfumeIdQuery, Guid> {
	public async Task<Guid> Handle(GetNextPerfumeIdQuery request, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		var from = await context.Perfumes.FindAsync(request.Id, cancellationToken);
		if (from == null) throw new NotFoundException();
		var next = await context.Perfumes
				.Where(x =>
					(string.Compare(x.House, from.House) > 0 ||
					(x.House == from.House && string.Compare(x.PerfumeName, from.PerfumeName) > 0))
					&& x.Ml > 0 
					&& (!x.PerfumeRatings.Any() || x.PerfumeRatings.Average(x => x.Rating) >= settings.MinimumRating)
				)
				.OrderBy(x => x.House)
				.ThenBy(x => x.PerfumeName)
				.FirstOrDefaultAsync();
		if (next != null) return next.Id;
		var first = await context.Perfumes.OrderBy(x => x.House)
			.ThenBy(x => x.PerfumeName)
			.FirstAsync();
		return first.Id;
	}
}
public class GetPreviousPerfumeHandler(PerfumeTrackerContext context)
		: IQueryHandler<GetPreviousPerfumeIdQuery, Guid> {
	public async Task<Guid> Handle(GetPreviousPerfumeIdQuery request, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		var from = await context.Perfumes.FindAsync(request.Id, cancellationToken);
		if (from == null) throw new NotFoundException();
		var next = await context.Perfumes
				.Where(x =>
					(string.Compare(x.House, from.House) < 0 ||
					(x.House == from.House && string.Compare(x.PerfumeName, from.PerfumeName) < 0))
					&& x.Ml > 0
				    && (!x.PerfumeRatings.Any() || x.PerfumeRatings.Average(x => x.Rating) >= settings.MinimumRating)
				)
				.OrderByDescending(x => x.House)
				.ThenByDescending(x => x.PerfumeName)
				.FirstOrDefaultAsync();
		if (next != null) return next.Id;
		var last = await context.Perfumes.OrderByDescending(x => x.House)
			.ThenByDescending(x => x.PerfumeName)
			.FirstAsync();
		return last.Id;
	}
}