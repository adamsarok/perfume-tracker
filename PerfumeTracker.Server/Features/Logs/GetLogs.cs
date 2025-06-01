
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Logs;
public record GetLogsResult(int TotalCount, IEnumerable<LogEntry> Items);
public class GetLogsEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/logs", async (PerfumeTrackerContext context, [FromQuery] int minLevel = 0,
			[FromQuery] int currentPage = 1,
			[FromQuery] int pageSize = 2
			) => {
				//var totalCount = await context.Database
				//	.SqlQuery<int>($@"SELECT COUNT(*) AS Value FROM ""log"" WHERE level >= {minLevel}")
				//	.SingleAsync();

				var totalCount = 0; //TODO

				int offset = (currentPage - 1) * pageSize;
				var logs = await context.Database
					.SqlQuery<LogEntry>($@"
                        SELECT 
                            message,
                            level,
                            timestamp,
                            exception,
                            properties
                        FROM ""log""
                        WHERE level >= {minLevel}
                        ORDER BY timestamp DESC
                        LIMIT {pageSize} OFFSET {offset}")
				.ToListAsync();
				return Results.Ok(new GetLogsResult(totalCount, logs));
			})
		.WithTags("Logs")
		.WithName("GetLogs");
	}
}

public record LogEntry(
	string Message,
	int Level,
	DateTime Timestamp,
	string? Exception,
	string? Properties
);