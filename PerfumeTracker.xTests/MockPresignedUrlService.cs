using Amazon.S3;
using PerfumeTracker.Server.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfumeTracker.xTests;
public class MockPresignedUrlService : IPresignedUrlService {
	public Uri? GetUrl(Guid? guid, HttpVerb httpVerb) => new("http://test.invalid");
}