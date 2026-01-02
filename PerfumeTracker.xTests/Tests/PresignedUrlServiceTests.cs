using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("PresignedUrlService Tests")]
public class PresignedUrlServiceCollection : ICollectionFixture<PresignedUrlServiceFixture>;

public class PresignedUrlServiceFixture : DbFixture {
	public PresignedUrlServiceFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await Task.CompletedTask;
	}
}

[Collection("PresignedUrlService Tests")]
public class PresignedUrlServiceTests {
	private readonly PresignedUrlServiceFixture _fixture;

	public PresignedUrlServiceTests(PresignedUrlServiceFixture fixture) {
		_fixture = fixture;
	}

	[Fact]
	public void GetPresignedUrl() {
		using var scope = _fixture.Factory.Services.CreateScope();
		var presignedService = scope.ServiceProvider.GetRequiredService<IPresignedUrlService>();
		var result = presignedService.GetUrl(Guid.NewGuid(), Amazon.S3.HttpVerb.GET);
		Assert.NotNull(result);
	}
}
