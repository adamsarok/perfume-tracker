using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Perfumes;

public record GetIdentifiedPerfumeQuery(string House, string PerfumeName) : IQuery<IdentifiedPerfume>;

public class GetIdentifiedPerfumeQueryValidator : AbstractValidator<GetIdentifiedPerfumeQuery> {
	public GetIdentifiedPerfumeQueryValidator() {
		RuleFor(x => x.House)
			.NotEmpty()
			.WithMessage("House is required");
		RuleFor(x => x.PerfumeName)
			.NotEmpty()
			.WithMessage("Perfume name is required");
	}
}

public class GetIdentifiedPerfumeEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/perfumes/identify/{house}/{perfumeName}", async (string house, string perfumeName, ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(new GetIdentifiedPerfumeQuery(house, perfumeName), cancellationToken);
			return Results.Ok(result);
		})
			.WithTags("Perfumes")
			.WithName("GetIdentifiedPerfume")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetIdentifiedPerfumeHandler(IPerfumeIdentifier perfumeIdentifier, PerfumeTrackerContext context)
	: IQueryHandler<GetIdentifiedPerfumeQuery, IdentifiedPerfume> {
	public async Task<IdentifiedPerfume> Handle(GetIdentifiedPerfumeQuery request, CancellationToken cancellationToken) {
		var userId = context.TenantProvider?.GetCurrentUserId() ?? throw new TenantNotSetException();
		return await perfumeIdentifier.GetIdentifiedPerfumeAsync(request.House, request.PerfumeName, userId, cancellationToken);
	}
}
