using Amazon.S3;
using PerfumeTracker.Server.Features.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfumeTracker.xTests;
public class MockPresignedUrlService : IPresignedUrlService {
	public string GetUrl(Guid guid, HttpVerb httpVerb) => "";
	public string GetUrl(string guid, HttpVerb httpVerb) => "";
}