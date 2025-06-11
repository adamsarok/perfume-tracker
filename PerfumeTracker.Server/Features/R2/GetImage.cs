
using Amazon.S3;
using PerfumeTracker.Server.Features.Common;

namespace PerfumeTracker.Server.Features.R2;
public class GetPresignedUrlEndpoint : ICarterModule {
	public record UploadResponse(Guid Guid, string PresignedUrl);
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/images/get-presigned-url/{id}", async (Guid id, IPresignedUrlService presignedUrlService) => {
			return presignedUrlService.GetUrl(id, HttpVerb.GET);
		})
			.WithTags("Images")
			.WithName("GetImageUrlDownload")
			.RequireAuthorization(Policies.READ);
		app.MapGet("/api/images/get-presigned-url", async (IPresignedUrlService presignedUrlService) => {
			var guid = Guid.NewGuid();
			return new UploadResponse(guid, presignedUrlService.GetUrl(guid, HttpVerb.PUT));
		})
			.WithTags("Images")
			.WithName("GetImageUrlUpload")
			.RequireAuthorization(Policies.READ);
	}
}