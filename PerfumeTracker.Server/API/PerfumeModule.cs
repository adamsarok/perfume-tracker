
using Carter;
using PerfumeTracker.Server.Dto;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Repo;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace FountainPensNg.Server.API {
    public class PerfumeModule : ICarterModule {
        public void AddRoutes(IEndpointRouteBuilder app) {
            const string PERFUMES = "Perfumes";
            app.MapGet("/api/perfumes/fulltext/{fulltext}", async (string fulltext, PerfumeRepo repo) =>
                await repo.GetPerfumesWithWorn(fulltext))
                .WithTags(PERFUMES)
                .WithName("GetPerfumesFulltext");

            app.MapGet("/api/perfumes/stats", async (PerfumeRepo repo) =>
               await repo.GetPerfumeStats())
               .WithTags(PERFUMES)
               .WithName("GetPerfumeStats");

            app.MapGet("/api/perfumes", async (PerfumeRepo repo) =>
                await repo.GetPerfumesWithWorn())
                .WithTags(PERFUMES)
                .WithName("GetPerfumes");

            app.MapGet("/api/perfumes/{id}", async (int id, PerfumeRepo repo) => {
                var perfume = await repo.GetPerfume(id);
                if (perfume == null) return Results.NotFound();
                return Results.Ok(perfume);
            }).WithTags(PERFUMES)
                .WithName("GetPerfume");

            app.MapPut("/api/perfumes/{id}", async (int id, PerfumeDto dto, PerfumeRepo repo) => {
                var result = await repo.UpdatePerfume(id, dto);
                return result.ResultType switch {
                    ResultTypes.Ok => Results.Ok(result.Perfume),
                    ResultTypes.NotFound => Results.NotFound(),
                    ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                    _ => Results.InternalServerError()
                };
            }).WithTags(PERFUMES)
                .WithName("UpdatePerfume");

            app.MapPut("/api/perfumes/imageguid", async (ImageGuidDto dto, PerfumeRepo repo) => {
                var result = await repo.UpdatePerfumeImageGuid(dto);
                return result.ResultType switch {
                    ResultTypes.Ok => Results.Ok(result.Perfume),
                    ResultTypes.NotFound => Results.NotFound(),
                    ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                    _ => Results.InternalServerError()
                };
            }).WithTags(PERFUMES)
                .WithName("UpdatePerfumeImageGuid");

            app.MapPost("/api/perfumes", async (PerfumeDto dto, PerfumeRepo repo) => {
                var result = await repo.AddPerfume(dto);
                return result.ResultType switch {
                    ResultTypes.Ok => Results.CreatedAtRoute("GetPerfume", new { id = result.Perfume?.Id }, result.Perfume),
                    ResultTypes.NotFound => Results.NotFound(),
                    ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                    _ => Results.InternalServerError()
                };
            }).WithTags(PERFUMES)
                .WithName("PostPerfume");

            app.MapDelete("/api/perfumes/{id}", async (int id, PerfumeRepo repo) => {
                var result = await repo.DeletePerfume(id);
                return result.ResultType switch {
                    ResultTypes.Ok => Results.NoContent(),
                    ResultTypes.NotFound => Results.NotFound(),
                    ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                    _ => Results.InternalServerError()
                };
            }).WithTags(PERFUMES)
                .WithName("DeletePerfume");
        }
    }
}
