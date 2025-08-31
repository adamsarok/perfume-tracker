using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Services.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfumeTracker.xTests;
public class PresignedUrlServiceTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public PresignedUrlServiceTests(WebApplicationFactory<Program> factory) : base(factory) { }
	[Fact]
	public void GetPresignedUrl() {
		using var scope = GetTestScope();
		var presignedService = scope.ServiceScope.ServiceProvider.GetRequiredService<IPresignedUrlService>();
		var result = presignedService.GetUrl(Guid.NewGuid(), Amazon.S3.HttpVerb.GET);
		Assert.NotNull(result);
	}
}
