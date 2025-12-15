using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;

namespace PerfumeTracker.Server.Services.Common;

public interface IPresignedUrlService {
	Uri? GetUrl(Guid? guid, HttpVerb httpVerb);
}
public class PresignedUrlService : IPresignedUrlService {
	private readonly R2Configuration r2Configuration;
	private readonly BasicAWSCredentials basicAWSCredentials;
	public PresignedUrlService(IOptions<R2Configuration> r2Options) {
		r2Configuration = r2Options.Value;
		basicAWSCredentials = new BasicAWSCredentials(r2Configuration.AccessKey, r2Configuration.SecretKey);
	}

	public Uri? GetUrl(Guid? guid, HttpVerb httpVerb) {
		if (guid == null || r2Configuration == null || !r2Configuration.IsEnabled) return null;
		using var s3Client = new AmazonS3Client(basicAWSCredentials, new AmazonS3Config {
			ServiceURL = $"https://{r2Configuration.AccountId}.r2.cloudflarestorage.com",
		});
		var presign = new GetPreSignedUrlRequest {
			BucketName = r2Configuration.BucketName,
			Key = guid.ToString(),
			Verb = httpVerb,
			Expires = DateTime.UtcNow.AddHours(r2Configuration.ExpirationHours),
		};
		return new Uri(s3Client.GetPreSignedURL(presign));
	}
}
