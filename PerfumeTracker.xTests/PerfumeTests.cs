using Amazon.S3;
using MediatR;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.Perfumes;
using PerfumeTracker.Server.Models;
using System.Net;
using System.Net.Http.Json;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
namespace PerfumeTracker.xTests;

public class PerfumeTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public PerfumeTests(WebApplicationFactory<Program> factory) : base(factory) { }
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = GetTestScope();
				var sql = "truncate table \"public\".\"Perfume\" cascade; " +
					"truncate table \"public\".\"Tag\" cascade; " +
					"truncate table \"public\".\"PerfumeTag\" cascade";
				await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync(sql);
				scope.PerfumeTrackerContext.Tags.AddRange(tagSeed);
				scope.PerfumeTrackerContext.Perfumes.AddRange(perfumeSeed);
				scope.PerfumeTrackerContext.PerfumeTags.AddRange(perfumeTagSeed);
				await scope.PerfumeTrackerContext.SaveChangesAsync();
				dbUp = true;
			}
		} finally {
			semaphore.Release();
		}
	}

	static List<Perfume> perfumeSeed = new List<Perfume> {
			new Perfume { Id = Guid.NewGuid(), House = "House1", PerfumeName = "Perfume1"
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("Perfume1")
			},
			new Perfume { Id = Guid.NewGuid(), House = "House2", PerfumeName = "Perfume2"
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("Perfume2")
			},
		};

	static List<Tag> tagSeed = new List<Tag> {
			new Tag { Id = Guid.NewGuid(), Color = "#FFFFFF", TagName = "Musky" },
			new Tag { Id = Guid.NewGuid(), Color = "#FF0000", TagName = "Woody" },
			new Tag { Id = Guid.NewGuid(), Color = "#FF0000", TagName = "Blue" }
		};

	static List<PerfumeTag> perfumeTagSeed = new List<PerfumeTag> {
			new PerfumeTag { Id = Guid.NewGuid(), Perfume = perfumeSeed[0], Tag = tagSeed[0] },
			new PerfumeTag { Id = Guid.NewGuid(), Perfume = perfumeSeed[1], Tag = tagSeed[1] }
		};

	[Fact]
	public async Task GetPerfume() {
		await PrepareData();
		using var scope = GetTestScope();
		var perfume = await scope.PerfumeTrackerContext.Perfumes.FirstAsync();
		var handler = new GetPerfumeHandler(scope.PerfumeTrackerContext, new MockPresignedUrlService());
		var response = await handler.Handle(new GetPerfumeQuery(perfume.Id), new CancellationToken());
		Assert.NotNull(response);
	}

	[Fact]
	public async Task GetPerfume_NotFound() {
		await PrepareData();
		using var scope = GetTestScope();
		var handler = new GetPerfumeHandler(scope.PerfumeTrackerContext, new MockPresignedUrlService());
		await Assert.ThrowsAsync<NotFoundException>(async () => await handler.Handle(new GetPerfumeQuery(Guid.NewGuid()), new CancellationToken()));
	}

	[Fact]
	public async Task GetPerfumes() {
		await PrepareData();
		using var scope = GetTestScope();
		var handler = new GetPerfumesWithWornHandler(scope.PerfumeTrackerContext, new MockPresignedUrlService());
		var perfumes = await handler.Handle(new GetPerfumesWithWornQuery(), new CancellationToken());
		Assert.NotNull(perfumes);
		Assert.NotEmpty(perfumes);
	}

	[Fact]
	public async Task UpdatePerfume() {
		await PrepareData();
		using var scope = GetTestScope();
		var handler = new UpdatePerfumeHandler(scope.PerfumeTrackerContext, MockSideEffectQueue.Object);
		var perfume = await scope.PerfumeTrackerContext.Perfumes.FirstAsync();
		var dto = new PerfumeUploadDto(perfume.House,
			perfume.PerfumeName,
			perfume.Ml,
			perfume.MlLeft,
			perfume.Autumn,
			perfume.Spring,
			perfume.Summer,
			perfume.Winter,
			new List<TagDto>() { tagSeed[2].Adapt<TagDto>() });
		var response = await handler.Handle(new UpdatePerfumeCommand(perfume.Id, dto), new CancellationToken());
		Assert.NotNull(response);
	}

	[Fact]
	public async Task DeletePerfume() {
		await PrepareData();
		using var scope = GetTestScope();
		var perfume = await scope.PerfumeTrackerContext.Perfumes.FirstAsync();
		var handler = new DeletePerfumeHandler(scope.PerfumeTrackerContext);
		var result = await handler.Handle(new DeletePerfumeCommand(perfume.Id), new CancellationToken());
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddPerfume() {
		await PrepareData();
		using var scope = GetTestScope();
		var dto = new PerfumeUploadDto("House3", "Perfume3", 50, 50, true, true, false, false, new());
		var handler = new AddPerfumeHandler(scope.PerfumeTrackerContext, MockSideEffectQueue.Object);
		var response = await handler.Handle(new AddPerfumeCommand(dto), new CancellationToken());
		Assert.NotNull(response);
	}

	//[Fact] TODO: test is flaky - runs in dev but not in github actions
	//public async Task GetFullText() {
	//	await PrepareData();
	//	var client = factory.CreateClient();
	//	var response = await client.GetAsync($"/api/perfumes/fulltext/{perfumeSeed[0].PerfumeName}");
	//	response.EnsureSuccessStatusCode();

	//	var perfumes = await response.Content.ReadFromJsonAsync<IEnumerable<PerfumeWithWornStatsDto>>();
	//	Assert.NotNull(perfumes);
	//	Assert.NotEmpty(perfumes);
	//}
}
