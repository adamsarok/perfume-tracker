
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using PerfumeTracker.Server.Features.Tags;
using System.Net;

namespace PerfumeTracker.Server.Features.R2;
public class GetImage : ICarterModule {
	public record UploadResponse(Guid Guid, string PresignedUrl);
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/images/get-presigned-url/{id}", async (Guid id, ISender sender, IConfiguration configuration) => {
			var r2Config = new R2Configuration(configuration);
			return GetPresignedUrl(r2Config, id, HttpVerb.GET);
		})
			.WithTags("Images")
			.WithName("GetImageUrlDownload")
			.RequireAuthorization(Policies.READ);
		app.MapGet("/api/images/get-presigned-url", async (ISender sender, IConfiguration configuration) => {
			var r2Config = new R2Configuration(configuration);
			var guid = Guid.NewGuid();
			return new UploadResponse(guid, GetPresignedUrl(r2Config, Guid.NewGuid(), HttpVerb.PUT));
		})
			.WithTags("Images")
			.WithName("GetImageUrlUpload")
			.RequireAuthorization(Policies.READ);
	}
	private static string GetPresignedUrl(R2Configuration r2Config, Guid guid, HttpVerb httpVerb) {
		var credentials = new BasicAWSCredentials(r2Config.AccessKey, r2Config.SecretKey);
		using var s3Client = new AmazonS3Client(credentials, new AmazonS3Config {
			ServiceURL = $"https://{r2Config.AccountId}.r2.cloudflarestorage.com",
		});
		var presign = new GetPreSignedUrlRequest {
			BucketName = r2Config.BucketName,
			Key = guid.ToString(),
			Verb = httpVerb,
			Expires = DateTime.Now.AddHours(r2Config.ExpirationHours),
		};
		return s3Client.GetPreSignedURL(presign);
	}
}
