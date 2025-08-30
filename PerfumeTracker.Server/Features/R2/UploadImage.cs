using Amazon.S3;
using ImageMagick;
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;
namespace PerfumeTracker.Server.Features.R2;

public record UploadResponse(Guid Guid);
public class UploadImageEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPut("/api/images/upload/{perfumeId}", async (Guid perfumeId,
			IFormFile file,
			R2Configuration configuration,
			PerfumeTrackerContext perfumeTrackerContext,
			UploadImageHandler uploadImageHandler,
			CancellationToken cancellationToken) => {
				if (!configuration.IsEnabled) return Results.InternalServerError("R2 not configured");
				var perfume = await perfumeTrackerContext.Perfumes.FindAsync(perfumeId) ?? throw new NotFoundException("Perfumes", perfumeId);
				if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded");
				if (file.Length > configuration.MaxFileSizeKb * 1024) {
					return Results.BadRequest($"File size exceeds the maximum limit of {configuration.MaxFileSizeKb}kb");
				}
				await using var stream = file.OpenReadStream();
				perfume.ImageObjectKeyNew = await uploadImageHandler.UploadImage(stream, cancellationToken);
				await perfumeTrackerContext.SaveChangesAsync(cancellationToken);
				return Results.Ok(new UploadResponse(perfume.ImageObjectKeyNew.Value));
			})
		.WithTags("Images")
		.WithName("UploadImage")
		.RequireAuthorization(Policies.WRITE)
		.DisableAntiforgery();
	}
}
public class UploadImageHandler(R2Configuration configuration, 
	IPresignedUrlService presignedUrlService,
	IHttpClientFactory httpClientFactory) { 
	public async Task<Guid> UploadImage(Stream stream, CancellationToken cancellationToken) {
		if (!configuration.IsEnabled) throw new ConfigEmptyException("R2 not configured");
		if (stream == null) throw new BadRequestException($"Image stream is null");
		try {
			using var image = new MagickImage(stream);
			if (!stream.CanSeek) throw new InvalidOperationException("Stream must support seeking for image processing");
			stream.Position = 0;
		} catch (Exception ex) {
			throw new BadRequestException($"Invalid image file: {ex.Message}");
		}
		var guid = Guid.NewGuid();
		var presignedUrl = presignedUrlService.GetUrl(guid, HttpVerb.PUT);
		using var httpClient = httpClientFactory.CreateClient();
		using var content = new StreamContent(stream);
		content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
		using var response = await httpClient.PutAsync(presignedUrl, content, cancellationToken);
		if (!response.IsSuccessStatusCode) {
			throw new InvalidOperationException($"Failed to upload image to R2: {response.StatusCode}");
		}
		return guid;
	}
}