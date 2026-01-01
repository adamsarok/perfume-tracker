using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.Perfumes;

public record GetNextPerfumeIdQuery(Guid Id) : IQuery<Guid>;
public record GetPreviousPerfumeIdQuery(Guid Id) : IQuery<Guid>;
public class GetAdjacentPerfumeIdEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/{id}/next", async (Guid id, ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GetNextPerfumeIdQuery(id), cancellationToken);
		})
			.WithTags("Perfumes")
			.WithName("GetNextPerfume")
			.RequireAuthorization(Policies.READ);
		app.MapGet("/api/perfumes/{id}/previous", async (Guid id, ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GetPreviousPerfumeIdQuery(id), cancellationToken);
		})
			.WithTags("Perfumes")
			.WithName("GetPreviousPerfume")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetNextPerfumeHandler(PerfumeTrackerContext context, IUserProfileService userProfileService)
		: IQueryHandler<GetNextPerfumeIdQuery, Guid> {
	public async Task<Guid> Handle(GetNextPerfumeIdQuery request, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var settings = await userProfileService.GetCurrentUserProfile(cancellationToken);
		var from = await context.Perfumes.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException("Perfumes", request.Id);
		var next = await context.Perfumes
				.Where(x =>
					(string.Compare(x.House, from.House) > 0 ||
					(x.House == from.House && string.Compare(x.PerfumeName, from.PerfumeName) > 0))
					&& x.MlLeft > 0
					&& (!x.PerfumeRatings.Any() || x.PerfumeRatings.Average(x => x.Rating) >= settings.MinimumRating)
				)
				.OrderBy(x => x.House)
				.ThenBy(x => x.PerfumeName)
				.FirstOrDefaultAsync(cancellationToken);
		if (next != null) return next.Id;
		var first = await context.Perfumes.OrderBy(x => x.House)
			.ThenBy(x => x.PerfumeName)
			.FirstAsync(cancellationToken);
		return first.Id;
	}
}
public class GetPreviousPerfumeHandler(PerfumeTrackerContext context, IUserProfileService userProfileService)
		: IQueryHandler<GetPreviousPerfumeIdQuery, Guid> {
	public async Task<Guid> Handle(GetPreviousPerfumeIdQuery request, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var settings = await userProfileService.GetCurrentUserProfile(cancellationToken);
		var from = await context.Perfumes.FindAsync([request.Id], cancellationToken) ?? throw new NotFoundException("Perfumes", request.Id);
		var next = await context.Perfumes
				.Where(x =>
					(string.Compare(x.House, from.House) < 0 ||
					(x.House == from.House && string.Compare(x.PerfumeName, from.PerfumeName) < 0))
					&& x.MlLeft > 0
					&& (!x.PerfumeRatings.Any() || x.PerfumeRatings.Average(x => x.Rating) >= settings.MinimumRating)
				)
				.OrderByDescending(x => x.House)
				.ThenByDescending(x => x.PerfumeName)
				.FirstOrDefaultAsync(cancellationToken);
		if (next != null) return next.Id;
		var last = await context.Perfumes.OrderByDescending(x => x.House)
			.ThenByDescending(x => x.PerfumeName)
			.FirstAsync(cancellationToken);
		return last.Id;
	}
}