
using Carter;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Repo;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace FountainPensNg.Server.API {
    public class FountainPenModule : ICarterModule {
        public void AddRoutes(IEndpointRouteBuilder app) {
            //TODO placeholder

            app.MapGet("/api/Perfumes", async (PerfumeRepo repo) =>
                await repo.GetPerfumesWithWorn())
                .WithTags("Perfumes")
                .WithName("GetPerfumes");

            app.MapGet("/api/Perfumes/{id}", async (int id, PerfumeRepo repo) => {
                var perfume = await repo.GetPerfumeWithWorn(id);
                if (perfume == null) return Results.NotFound();
                return Results.Ok(perfume);
            }).WithTags("Perfumes")
                .WithName("GetPerfume");

            app.MapPut("/api/Perfumes/{id}", async (int id, PerfumeDTO dto, PerfumeRepo repo) => {
                var result = await repo.UpdatePerfume(id, dto);
                return result.ResultType switch {
                    ResultTypes.Ok => Results.Ok(result.Perfume),
                    ResultTypes.NotFound => Results.NotFound(),
                    ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                    _ => Results.InternalServerError()
                };
            }).WithTags("Perfumes")
                .WithName("PutPerfume");

            app.MapPost("/api/Perfumes", async (PerfumeDTO dto, PerfumeRepo repo) => {
                var result = await repo.AddPerfume(dto);
                return result.ResultType switch {
                    ResultTypes.Ok => Results.CreatedAtRoute("GetPerfume", new { id = result?.Perfume?.Id }, result?.Perfume),
                    ResultTypes.NotFound => Results.NotFound(),
                    ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                    _ => Results.InternalServerError()
                };
            }).WithTags("Perfumes")
                .WithName("PostPerfume");

            app.MapDelete("/api/Perfumes/{id}", async (int id, PerfumeRepo repo) => {
                var result = await repo.DeletePerfume(id);
                return result.ResultType switch {
                    ResultTypes.Ok => Results.NoContent(),
                    ResultTypes.NotFound => Results.NotFound(),
                    ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                    _ => Results.InternalServerError()
                };
            }).WithTags("Perfumes")
                .WithName("DeletePerfume");
        }
    }
}
