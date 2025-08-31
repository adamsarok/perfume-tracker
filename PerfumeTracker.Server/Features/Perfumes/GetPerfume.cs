using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.Perfumes;
public enum Direction {	Next, Previous }
public record GetPerfumeQuery(Guid Id) : IQuery<PerfumeWithWornStatsDto>;
public class GetPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/{id}", async (Guid id, ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GetPerfumeQuery(id), cancellationToken);
		})
			.WithTags("Perfumes")
			.WithName("GetPerfume")
			.RequireAuthorization(Policies.READ);
	}
}
public class GetPerfumeHandler(PerfumeTrackerContext context, IPresignedUrlService presignedUrlService)
		: IQueryHandler<GetPerfumeQuery, PerfumeWithWornStatsDto> {
	public async Task<PerfumeWithWornStatsDto> Handle(GetPerfumeQuery request, CancellationToken cancellationToken) {
		var settings = await context.UserProfiles.FirstAsync(cancellationToken);
		var p = await context
			.Perfumes
			.Include(x => x.PerfumeEvents)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Include(x => x.PerfumeRatings)
			.Where(p => p.Id == request.Id)
			.Select(p => p.ToPerfumeWithWornStatsDto(settings, presignedUrlService))
			.AsSplitQuery()
			.AsNoTracking()
			.FirstOrDefaultAsync(cancellationToken);
		return p ?? throw new NotFoundException();
	}
}