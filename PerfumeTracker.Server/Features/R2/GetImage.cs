﻿
using Amazon.S3;
using PerfumeTracker.Server.Features.Common;

namespace PerfumeTracker.Server.Features.R2;
public class GetPresignedUrlEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/images/get-presigned-url/{id}", async (Guid id, IPresignedUrlService presignedUrlService) => {
			return presignedUrlService.GetUrl(id, HttpVerb.GET);
		})
			.WithTags("Images")
			.WithName("GetImageUrlDownload")
			.RequireAuthorization(Policies.READ);
	}
}