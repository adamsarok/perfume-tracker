using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using PerfumeTracker.Server.Config;

namespace PerfumeTracker.Server.Features.Common;
public interface IPresignedUrlService {
	string GetUrl(Guid guid, HttpVerb httpVerb);
	string GetUrl(string guid, HttpVerb httpVerb);
}
public class PresignedUrlService : IPresignedUrlService {
	private readonly R2Configuration r2Configuration;
	private readonly BasicAWSCredentials basicAWSCredentials;
	public PresignedUrlService(R2Configuration r2Configuration) {
		basicAWSCredentials = new BasicAWSCredentials(r2Configuration.AccessKey, r2Configuration.SecretKey); ;
		this.r2Configuration = r2Configuration;
	}

	public string GetUrl(Guid guid, HttpVerb httpVerb) {
		using var s3Client = new AmazonS3Client(basicAWSCredentials, new AmazonS3Config {
			ServiceURL = $"https://{r2Configuration.AccountId}.r2.cloudflarestorage.com",
		});
		var presign = new GetPreSignedUrlRequest {
			BucketName = r2Configuration.BucketName,
			Key = guid.ToString(),
			Verb = httpVerb,
			Expires = DateTime.UtcNow.AddHours(r2Configuration.ExpirationHours),
		};
		return s3Client.GetPreSignedURL(presign);
	}

	public string GetUrl(string guid, HttpVerb httpVerb) {
		if (string.IsNullOrEmpty(guid)) return "";
		return GetUrl(new Guid(guid), httpVerb);
	}
}
