
using Microsoft.AspNetCore.Mvc;

namespace PerfumeTracker.Server.Features.Logs;

public class GetLogsEndpoint : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
		app.MapGet("/api/logs", async (PerfumeTrackerContext context, [FromQuery] int minLevel = 0) => {
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
                        LIMIT 100")
				.ToListAsync();

			return Results.Ok(logs);
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