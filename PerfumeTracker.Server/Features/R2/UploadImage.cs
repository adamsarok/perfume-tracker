using PerfumeTracker.Server.Features.Common;
using SixLabors.ImageSharp;
using Amazon.S3;
using Microsoft.AspNetCore.Http;

namespace PerfumeTracker.Server.Features.R2;

public record UploadResponse(Guid Guid);
public class UploadImageEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/images/upload/{perfumeId}", async (Guid perfumeId,
			IFormFile file,
			IPresignedUrlService presignedUrlService,
			ILogger<UploadImageEndpoint> logger,
			R2Configuration configuration,
			PerfumeTrackerContext perfumeTrackerContext) => {
				var perfume = await perfumeTrackerContext.Perfumes.FindAsync(perfumeId);
				if (perfume == null) return Results.BadRequest("Perfume Id not found");
				if (file == null || file.Length == 0) return Results.BadRequest("No file uploaded");
				if (file.Length > configuration.MaxFileSizeKb * 1024) {
					return Results.BadRequest($"File size exceeds the maximum limit of {configuration.MaxFileSizeKb}kb");
				}
				using var stream = file.OpenReadStream();
				if (!await IsValidImageFile(stream, logger)) return Results.BadRequest("Invalid image file");

				var guid = Guid.NewGuid();
				var presignedUrl = presignedUrlService.GetUrl(guid, HttpVerb.PUT);

				using var httpClient = new HttpClient();
				var response = await httpClient.PutAsync(presignedUrl, new StreamContent(stream));
				if (!response.IsSuccessStatusCode) {
					logger.LogError("Failed to upload image to R2: {StatusCode}", response.StatusCode);
					return Results.Problem("Failed to upload image");
				}
				perfume.ImageObjectKey = guid.ToString();
				await perfumeTrackerContext.SaveChangesAsync();
				return Results.Ok(new UploadResponse(guid));
			})
		.WithTags("Images")
		.WithName("UploadImage")
		.RequireAuthorization(Policies.WRITE);
	}

	private async Task<bool> IsValidImageFile(Stream stream, ILogger<UploadImageEndpoint> logger) {
		try {
			using var image = await Image.LoadAsync(stream);
			stream.Position = 0;
			return true;
		} catch (Exception ex) {
			logger.Log(LogLevel.Error, ex, "Upload is not a valid image file");
			return false;
		}
	}
}