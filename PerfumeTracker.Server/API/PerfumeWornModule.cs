using System;
using Carter;
using PerfumeTracker.Server.Dto;
using PerfumeTracker.Server.Repo;

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

        app.MapPost("/api/perfumeworns", async (PerfumeWornUploadDto dto, PerfumeWornRepo repo) => {
            var result = await repo.AddPerfumeWorn(dto);
			return Results.CreatedAtRoute("GetPerfumeWorns", new { id = result.Id }, result);
        }).WithTags("PerfumeWorns")
            .WithName("PostPerfumeWorn");

        app.MapDelete("/api/perfumeworns/{id}", async (int id, PerfumeWornRepo repo) => {
            await repo.DeletePerfumeWorn(id);
			return Results.NoContent();
        }).WithTags("PerfumeWorns")
            .WithName("DeletePerfumeWorn");
    }
}

