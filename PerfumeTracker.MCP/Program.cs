using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions => {
	consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

string? conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn)) throw new ConfigEmptyException("Connection string is empty");

builder.Services.AddDbContext<PerfumeTrackerContext>(opt => opt.UseNpgsql(conn));
builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithToolsFromAssembly();

await builder.Build().RunAsync();