using ModelContextProtocol.Server;
using PerfumeTracker.Server.Repo;
using System.ComponentModel;
using System.Text.Json;

namespace PerfumeTracker.MCP;

[McpServerToolType]
public static class Perfumes {
	[McpServerTool, Description("Test")]
	public static async Task<string> Test() {
		return "test";
	}

	//TODO
	//[McpServerTool, Description("Get perfume by ID")]
	//public static async Task<string> GetPerfume(PerfumeRepo repo, int id) {
	//	var p = await repo.GetPerfume(id);
	//	return JsonSerializer.Serialize(p);
	//}
	
	//[McpServerTool, Description("Get perfumes")]
	//public static async Task<string> GetPerfumes(PerfumeRepo repo) {
	//	var p = await repo.GetPerfumesWithWorn();
	//	return JsonSerializer.Serialize(p);
	//}
}