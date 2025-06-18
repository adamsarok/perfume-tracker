using Hangfire;

namespace PerfumeTracker.Server.Features.Jobs;

public class ScheduleJob : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/job/enqueue", async (IBackgroundJobClient backgroundJobClient) => {
			backgroundJobClient.Enqueue(() => Console.WriteLine("Hangfire job fired!"));
			return Results.Ok();
		}).WithTags("Jobs")
			.WithName("EnqueueJob")
			.AllowAnonymous();
		app.MapPost("/api/job/schedule", async (IBackgroundJobClient backgroundJobClient) => {
			backgroundJobClient.Schedule(() => Console.WriteLine("Scheduled job executed."), TimeSpan.FromMinutes(1));
			return Results.Ok();
		}).WithTags("Jobs")
			.WithName("ScheduleJob")
			.AllowAnonymous();
	}
}