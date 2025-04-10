using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PerfumeTracker.Server.Models;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Repo;
using System.ComponentModel;
using System.Text.Json;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.AddConsole(consoleLogOptions => {
	consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

string? conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn)) throw new ConfigEmptyException("Connection string is empty");

builder.Services.AddDbContext<PerfumetrackerContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddScoped<PerfumeRepo>();
builder.Services
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithToolsFromAssembly();

await builder.Build().RunAsync();



[McpServerToolType]
public static class Perfumes {
	[McpServerTool, Description("Test")]
	public static async Task<string> Test() {
		return "test";
	}

	[McpServerTool, Description("Get perfume by ID")]
	public static async Task<string> GetPerfume(PerfumeRepo repo, int id) {
		var p = await repo.GetPerfume(id);
		return JsonSerializer.Serialize(p);
	}
	
	[McpServerTool, Description("Get perfumes")]
	public static async Task<string> GetPerfumes(PerfumeRepo repo) {
		var p = await repo.GetPerfumesWithWorn();
		return JsonSerializer.Serialize(p);
	}
}