using MediatR;
using ModelContextProtocol.Server;
using PerfumeTracker.Server.Features.Perfumes;
using System.ComponentModel;
using System.Text.Json;

namespace PerfumeTracker.MCP;

[McpServerToolType]
public static class Perfumes {
	[McpServerTool, Description("Get perfumes")]
	public static async Task<string> GetPerfumes(ISender sender) {
		var p = await sender.Send(new GetPerfumesWithWornQuery());
		return JsonSerializer.Serialize(p);
	}
}