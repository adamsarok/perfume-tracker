
using Carter;
using PerfumeTracker.Server.Dto;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Repo;

namespace FountainPensNg.Server.API {
    public class PerfumePlaylistModule : ICarterModule {
		public void AddRoutes(IEndpointRouteBuilder app) {
            const string tag = "PerfumePlaylists";
            app.MapGet("/api/perfume-playlists", async (PerfumePlaylistRepo repo) =>
                await repo.GetPerfumePlaylists())
                .WithTags(tag)
                .WithName("GetPerfumePlaylists");

            app.MapGet("/api/perfume-playlists/{name}", async (string name, PerfumePlaylistRepo repo) => 
                await repo.GetPerfumePlaylist(name))
                .WithTags(tag)
                .WithName("GetPerfumePlaylist");

            app.MapPut("/api/perfume-playlists", async (PerfumePlaylistDto dto, PerfumePlaylistRepo repo) => 
                await repo.UpdatePerfumePlaylist(dto))
                .WithTags(tag)
                .WithName("UpdatePerfumePlaylist");

            app.MapPost("/api/perfume-playlists", async (PerfumePlaylistDto dto, PerfumePlaylistRepo repo) => {
                var result = await repo.AddPerfumePlaylist(dto);
				return Results.CreatedAtRoute("GetPerfumePlaylist", new { name = result.Name }, result);
            }).WithTags(tag)
                .WithName("PostPerfumePlaylist");

            app.MapDelete("/api/perfume-playlists/{name}", async (string name, PerfumePlaylistRepo repo) => {
                await repo.DeletePerfumePlaylist(name);
				return Results.NoContent();
            }).WithTags(tag)
                .WithName("DeletePerfumePlaylist");
        }
    }
}
