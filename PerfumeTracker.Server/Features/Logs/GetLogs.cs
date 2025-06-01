
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Logs;
public record GetLogsResult(int TotalCount, IEnumerable<LogEntry> Items);
public class GetLogsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/logs", async (PerfumeTrackerContext context, [FromQuery] int minLevel = 0,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10
			) => {
				var totalCount = await context.LogEntries.CountAsync(x => x.Level >= minLevel);
				int offset = (page - 1) * pageSize;
				var logs = await context.LogEntries
					.Where(x => x.Level >= minLevel)
					.Skip(offset)
					.Take(pageSize)
					.ToListAsync();
				return Results.Ok(new GetLogsResult(totalCount, logs));
			})
		.WithTags("Logs")
		.WithName("GetLogs");
	}
}
