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
			UploadImageHandler uploadImageHandler) => {
				if (!configuration.IsEnabled) return Results.InternalServerError("R2 not configured");
				var perfume = await perfumeTrackerContext.Perfumes.FindAsync(perfumeId) ?? throw new NotFoundException("Perfumes", perfumeId);
				if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded");
				if (file.Length > configuration.MaxFileSizeKb * 1024) {
					return Results.BadRequest($"File size exceeds the maximum limit of {configuration.MaxFileSizeKb}kb");
				}
				await using var stream = file.OpenReadStream();
				perfume.ImageObjectKeyNew = await uploadImageHandler.UploadImage(stream);
				await perfumeTrackerContext.SaveChangesAsync();
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
	public async Task<Guid> UploadImage(Stream stream) {
		if (!configuration.IsEnabled) throw new ConfigEmptyException("R2 not configured");
		try {
			using var image = new MagickImage(stream);
			stream.Position = 0;
		} catch (Exception ex) {
			throw new BadRequestException($"Invalid image file: {ex.Message}");
		}
		var guid = Guid.NewGuid();
		var presignedUrl = presignedUrlService.GetUrl(guid, HttpVerb.PUT);
		var httpClient = httpClientFactory.CreateClient();
		var response = await httpClient.PutAsync(presignedUrl, new StreamContent(stream));
		if (!response.IsSuccessStatusCode) {
			throw new InvalidOperationException($"Failed to upload image to R2: {response.StatusCode}");
		}
		return guid;
	}
}