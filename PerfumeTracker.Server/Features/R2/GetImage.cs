
using Amazon.S3;
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.R2;
public class GetPresignedUrlEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/images/get-presigned-url/{id}", (Guid id, IPresignedUrlService preSignedUrlService) =>
			preSignedUrlService.GetUrl(id, HttpVerb.GET)
		)
			.WithTags("Images")
			.WithName("GetImageUrlDownload")
			.RequireAuthorization(Policies.READ);
	}
}