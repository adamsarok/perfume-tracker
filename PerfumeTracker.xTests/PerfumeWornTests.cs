using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.PerfumeEvents;
using PerfumeTracker.Server.Features.Perfumes;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests;

public class PerfumeWornTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public PerfumeWornTests(WebApplicationFactory<Program> factory) : base(factory) { }
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = GetTestScope();
				var sql = "truncate table \"public\".\"PerfumeEvent\" cascade; truncate table \"public\".\"Perfume\" cascade;";
				await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync(sql);
				scope.PerfumeTrackerContext.Perfumes.AddRange(perfumeSeed);
				scope.PerfumeTrackerContext.PerfumeEvents.Add(new PerfumeEvent() {
					Perfume = perfumeSeed[0],
					CreatedAt = DateTime.UtcNow,
					EventDate = DateTime.UtcNow,
					Type = PerfumeEvent.PerfumeEventType.Worn
				});
				scope.PerfumeTrackerContext.PerfumeEvents.Add(new PerfumeEvent() {
					Perfume = perfumeSeed[1],
					CreatedAt = DateTime.UtcNow.AddDays(-1),
					EventDate = DateTime.UtcNow.AddDays(-1),
					Type = PerfumeEvent.PerfumeEventType.Worn
				});
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
			new Perfume { Id = Guid.NewGuid(), House = "House2", PerfumeName = "NotWornPerfume"
				,FullText = NpgsqlTypes.NpgsqlTsVector.Parse("NotWornPerfume")
			},
		};

	[Fact]
	public async Task GetPerfumeWorns() {
		await PrepareData();
		using var scope = GetTestScope();
		var handler = new GetWornPerfumesHandler(scope.PerfumeTrackerContext, new MockPresignedUrlService());
		var result = await handler.Handle(new GetWornPerfumesQuery(0, 20), new CancellationToken());
		Assert.NotNull(result);
		Assert.NotEmpty(result);
	}

	[Fact]
	public async Task DeletePerfumeWorn() {
		await PrepareData();
		using var scope = GetTestScope();
		var worn = await scope.PerfumeTrackerContext.PerfumeEvents.FirstAsync();
		var handler = new DeletePerfumeEventHandler(scope.PerfumeTrackerContext);
		var result = await handler.Handle(new DeletePerfumeEventCommand(worn.Id), new CancellationToken());
		Assert.True(result.IsDeleted);
	}

	[Fact]
	public async Task AddPerfumeWorn() {
		await PrepareData();
		using var scope = GetTestScope();
		var dto = new PerfumeEventUploadDto(perfumeSeed[2].Id, DateTime.UtcNow, PerfumeEvent.PerfumeEventType.Worn, 0.05m, false);
		var handler = new AddPerfumeEventHandler(scope.PerfumeTrackerContext, MockSideEffectQueue.Object);
		var result = await handler.Handle(new AddPerfumeEventCommand(dto), new CancellationToken());
		Assert.True(await scope.PerfumeTrackerContext.PerfumeEvents.AnyAsync(x => x.Id == result.Id));
	}
}
