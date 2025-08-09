using Mapster;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.PerfumeRandoms;
using System.Net.Http.Json;

namespace PerfumeTracker.xTests;

public class RandomPerfumeTests : TestBase, IClassFixture<WebApplicationFactory<Program>> {
	public RandomPerfumeTests(WebApplicationFactory<Program> factory) : base(factory) { }
	static bool dbUp = false;
	private static readonly SemaphoreSlim semaphore = new SemaphoreSlim(1);
	private async Task PrepareData() {          //fixtures don't have DI
		await semaphore.WaitAsync();
		try {
			if (!dbUp) {
				using var scope = GetTestScope();
				var sql = "truncate table \"public\".\"PerfumeEvent\" cascade; truncate table \"public\".\"Perfume\" cascade;  truncate table \"public\".\"PerfumeRandom\" cascade;";
				await scope.PerfumeTrackerContext.Database.ExecuteSqlRawAsync(sql);
				scope.PerfumeTrackerContext.Perfumes.AddRange(perfumeSeed);
				scope.PerfumeTrackerContext.PerfumeEvents.Add(new PerfumeEvent() {
					Perfume = perfumeSeed[0],
					CreatedAt = DateTime.UtcNow,
					EventDate = DateTime.UtcNow,
					Type = PerfumeEvent.PerfumeEventType.Worn
				});
				scope.PerfumeTrackerContext.PerfumeRandoms.Add(new PerfumeRandoms() {
					Perfume = perfumeSeed[1],
					CreatedAt = DateTime.UtcNow.AddDays(-2)
				});
				await scope.PerfumeTrackerContext.SaveChangesAsync();
				dbUp = true;
			}
		}
		finally {
			semaphore.Release();
		}
	}

	static List<Perfume> perfumeSeed = new List<Perfume> {
			new Perfume { Id = Guid.NewGuid(), House = "House1", PerfumeName = "Perfume1" },
			new Perfume { Id = Guid.NewGuid(), House = "House2", PerfumeName = "Perfume2" },
			new Perfume { Id = Guid.NewGuid(), House = "House2", PerfumeName = "NotWornPerfume" },
		};

	[Fact]
	public async Task GetPerfumeSuggestion() {
		await PrepareData();
		using var scope = GetTestScope();
		var handler = new GetRandomPerfumeHandler(scope.PerfumeTrackerContext, MockSideEffectQueue.Object);
		var response = await handler.Handle(new GetRandomPerfumeQuery(), CancellationToken.None);
		Assert.NotNull(response);
	}

}
