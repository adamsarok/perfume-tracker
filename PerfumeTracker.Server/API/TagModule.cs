
using Carter;
using PerfumeTracker.Server.Repo;

namespace FountainPensNg.Server.API;
public class TagModule : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/tags/stats", async (TagRepo repo) =>
			await repo.GetTagStats())
			.WithTags("Tags")
			.WithName("GetTagStats");

		app.MapGet("/api/tags", async (TagRepo repo) =>
			await repo.GetTags())
			.WithTags("Tags")
			.WithName("GetTags");

		app.MapGet("/api/tags/{id}", async (int id, TagRepo repo) =>
			await repo.GetTag(id))
			.WithTags("Tags")
			   .WithName("GetTag");

		app.MapPut("/api/tags/{id}", async (int id, TagDto dto, TagRepo repo) =>
			await repo.UpdateTag(id, dto))
			.WithTags("Tags")
			.WithName("UpdateTag");

		app.MapPost("/api/tags", async (TagDto dto, TagRepo repo) => {
			var result = await repo.AddTag(dto);
			return Results.CreatedAtRoute("GetTag", new { id = result.Id }, result);
		}).WithTags("Tags")
		   .WithName("AddTag");

		app.MapDelete("/api/tags/{id}", async (int id, TagRepo repo) => {
			await repo.DeleteTag(id);
			return Results.NoContent();
		}).WithTags("Tags")
		   .WithName("DeleteTag");
	}
}
