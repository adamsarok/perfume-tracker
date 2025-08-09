using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.PerfumeRatings;
public record PerfumeRatingUploadDto(Guid PerfumeId, decimal Rating, string Comment);
public record PerfumeRatingDownloadDto(Guid PerfumeId, Guid Id, decimal Rating, string Comment, DateTime RatingDate, bool IsDeleted);
public record AddPerfumeRatingCommand(PerfumeRatingUploadDto Dto) : ICommand<PerfumeRatingDownloadDto>;
public class AddPerfumeRatingCommandValidator : AbstractValidator<AddPerfumeRatingCommand> {
	public AddPerfumeRatingCommandValidator() {
		RuleFor(x => x.Dto.PerfumeId).NotEmpty();
		RuleFor(x => x.Dto.Rating).InclusiveBetween(0, 10);
	}
}
public class AddPerfumeRatingEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/perfumes/{perfumeId}/ratings", async (PerfumeRatingUploadDto dto, ISender sender) => {
			var result = await sender.Send(new AddPerfumeRatingCommand(dto));
			return Results.CreatedAtRoute("GetPerfumeRatings", new { perfumeId = dto.PerfumeId }, result);
		}).WithTags("PerfumeRatings")
			.WithName("PostPerfumeRating")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class AddPerfumeRatingHandler(PerfumeTrackerContext context) : ICommandHandler<AddPerfumeRatingCommand, PerfumeRatingDownloadDto> {
	public async Task<PerfumeRatingDownloadDto> Handle(AddPerfumeRatingCommand request, CancellationToken cancellationToken) {
		if (context.TenantProvider?.GetCurrentUserId() == null) throw new TenantNotSetException();
		var evt = request.Dto.Adapt<PerfumeRating>();
		if (await context.Perfumes.FindAsync(evt.PerfumeId) == null) throw new NotFoundException("Perfumes", evt.PerfumeId);
		context.PerfumeRatings.Add(evt);
		var result = evt.Adapt<PerfumeRatingDownloadDto>();
		await context.SaveChangesAsync();
		return result;
	}
}