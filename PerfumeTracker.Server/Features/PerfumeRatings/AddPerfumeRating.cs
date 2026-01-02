using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.PerfumeRatings.Services;

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
		app.MapPost("/api/perfumes/{perfumeId}/ratings", async (PerfumeRatingUploadDto dto, ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(new AddPerfumeRatingCommand(dto), cancellationToken);
			return Results.CreatedAtRoute("GetPerfumeRatings", new { perfumeId = dto.PerfumeId }, result);
		}).WithTags("PerfumeRatings")
			.WithName("PostPerfumeRating")
			.RequireAuthorization(Policies.WRITE);
	}
}
public class AddPerfumeRatingHandler(IRatingService ratingService) : ICommandHandler<AddPerfumeRatingCommand, PerfumeRatingDownloadDto> {
	public async Task<PerfumeRatingDownloadDto> Handle(AddPerfumeRatingCommand request, CancellationToken cancellationToken) {
		var result = await ratingService.AddPerfumeRating(
			perfumeId: request.Dto.PerfumeId,
			rating: request.Dto.Rating,
			comment: request.Dto.Comment,
			cancellationToken: cancellationToken);
		return result.Adapt<PerfumeRatingDownloadDto>();
	}
}