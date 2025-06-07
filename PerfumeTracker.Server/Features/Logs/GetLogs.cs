
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Logs;
public class GetLogsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/logs", async (PerfumeTrackerContext context, [FromQuery] int minLevel = 0,
			[FromQuery] int page = 1,
			[FromQuery] int pageSize = 10
			) => {
				var totalCount = await context.LogEntries.CountAsync(x => x.Level >= minLevel);
				int offset = (page - 1) * pageSize;
				//TODO: log does not have UserID - add with global filter
				var logs = await context.LogEntries
					.Where(x => x.Level >= minLevel)
					.Skip(offset)
					.Take(pageSize)
					.ToListAsync();
				return Results.Ok(new PaginatedResult<LogEntry>(page, pageSize, totalCount, logs));
			})
		.WithTags("Logs")
		.WithName("GetLogs")
		.RequireAuthorization(Policies.ADMIN);
	}
}
