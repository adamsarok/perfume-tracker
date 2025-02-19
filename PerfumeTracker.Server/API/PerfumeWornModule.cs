using System;
using Carter;
using PerfumeTracker.Server.DTO;
using PerfumeTracker.Server.Repo;
using static PerfumeTrackerAPI.Repo.ResultType;

namespace PerfumeTracker.Server.API;

public class PerfumeWornModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        app.MapGet("/api/perfumeworns", async (int cursor, int pageSize, PerfumeWornRepo repo) =>
            await repo.GetPerfumesWithWorn(cursor, pageSize))
            .WithTags("PerfumeWorns")
            .WithName("GetPerfumeWorns");

        app.MapGet("/api/perfumeworns/perfumesbefore/{daysBefore}", async (int daysBefore, PerfumeWornRepo repo) =>
            await repo.GetWornPerfumeIDs(daysBefore))
            .WithTags("PerfumeWorns")
            .WithName("GetPerfumesBefore");

        app.MapPost("/api/perfumeworns", async (PerfumeWornUploadDTO dto, PerfumeWornRepo repo) => {
            var result = await repo.AddPerfumeWorn(dto);
            return result.ResultType switch {
                ResultTypes.Ok => Results.CreatedAtRoute("GetPerfumeWorns", new { id = result.worn?.Id }, result.worn),
                ResultTypes.NotFound => Results.NotFound(),
                ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                _ => Results.InternalServerError()
            };
        }).WithTags("PerfumeWorns")
            .WithName("PostPerfumeWorn");

        app.MapDelete("/api/perfumeworns/{id}", async (int id, PerfumeWornRepo repo) => {
            var result = await repo.DeletePerfumeWorn(id);
            return result.ResultType switch {
                ResultTypes.Ok => Results.NoContent(),
                ResultTypes.NotFound => Results.NotFound(),
                ResultTypes.BadRequest => Results.BadRequest(result.ErrorMsg),
                _ => Results.InternalServerError()
            };
        }).WithTags("PerfumeWorns")
            .WithName("DeletePerfumeWorn");
    }
}

