using System;
using Carter;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class PerfumeWornModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        //TODO placeholder

        app.MapGet("/api/perfumeworns", async (int cursor, int pageSize, PerfumeWornRepo repo) =>
            await repo.GetPerfumesWithWorn(cursor, pageSize))
            .WithTags("PerfumeWorns")
            .WithName("GetPerfumeWorns");

        app.MapGet("/api/perfumeworns/perfumesbefore/{daysBefore}", async (int daysBefore, PerfumeWornRepo repo) =>
            await repo.GetWornPerfumeIDs(daysBefore))
            .WithTags("PerfumeWorns")
            .WithName("GetPerfumesBefore");

        // app.MapGet("/api/PerfumeWorns/stats", async (PerfumeWornRepo repo) =>
        //    await repo.GetPerfumeWornstats())
        //    .WithTags("PerfumeWorns")
        //    .WithName("GetPerfumeWornstats");

        // app.MapGet("/api/PerfumeWorns", async (PerfumeWornRepo repo) =>
        //     await repo.GetPerfumeWornsWithWorn())
        //     .WithTags("PerfumeWorns")
        //     .WithName("GetPerfumeWorns");

        // app.MapGet("/api/PerfumeWorns/{id}", async (int id, PerfumeWornRepo repo) => {
        //     var perfume = await repo.GetPerfumeWithWorn(id);
        //     if (perfume == null) return Results.NotFound();
        //     return Results.Ok(perfume);
        // }).WithTags("PerfumeWorns")
        //     .WithName("GetPerfume");

        // app.MapPut("/api/PerfumeWorns/{id}", async (int id, PerfumeDTO dto, PerfumeWornRepo repo) => {
        //     var result = await repo.UpdatePerfume(id, dto);
        //     return result.ResultType switch {
        //         ResultTypes.Ok => Results.Ok(result.Perfume),
        //         ResultTypes.NotFound => Results.NotFound(),
        //         ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
        //         _ => Results.InternalServerError()
        //     };
        // }).WithTags("PerfumeWorns")
        //     .WithName("PutPerfume");

        // app.MapPost("/api/PerfumeWorns", async (PerfumeDTO dto, PerfumeWornRepo repo) => {
        //     var result = await repo.AddPerfume(dto);
        //     return result.ResultType switch {
        //         ResultTypes.Ok => Results.CreatedAtRoute("GetPerfume", new { id = result?.Perfume?.Id }, result?.Perfume),
        //         ResultTypes.NotFound => Results.NotFound(),
        //         ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
        //         _ => Results.InternalServerError()
        //     };
        // }).WithTags("PerfumeWorns")
        //     .WithName("PostPerfume");

        // app.MapDelete("/api/PerfumeWorns/{id}", async (int id, PerfumeWornRepo repo) => {
        //     var result = await repo.DeletePerfume(id);
        //     return result.ResultType switch {
        //         ResultTypes.Ok => Results.NoContent(),
        //         ResultTypes.NotFound => Results.NotFound(),
        //         ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
        //         _ => Results.InternalServerError()
        //     };
        // }).WithTags("PerfumeWorns")
        //     .WithName("DeletePerfume");
    }
}

