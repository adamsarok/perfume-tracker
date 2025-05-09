using System;
using Carter;
using PerfumeTracker.Server.Dto;
using PerfumeTracker.Server.Repo;

namespace PerfumeTracker.Server.API;

public class PerfumeWornModule : ICarterModule {
    public void AddRoutes(IEndpointRouteBuilder app) {
        app.MapGet("/api/perfume-events/worn-perfumes", async (int cursor, int pageSize, PerfumeEventsRepo repo) =>
            await repo.GetPerfumesWithWorn(cursor, pageSize))
            .WithTags("PerfumeWorns")
            .WithName("GetPerfumeWorns");

        app.MapGet("/api/perfume-events/worn-perfumes/{daysBefore}", async (int daysBefore, PerfumeEventsRepo repo) =>
            await repo.GetWornPerfumeIDs(daysBefore))
            .WithTags("PerfumeWorns")
            .WithName("GetPerfumesBefore");

		app.MapPost("/api/perfume-events", async (PerfumeEventUploadDto dto, PerfumeEventsRepo repo) => {
            var result = await repo.AddPerfumeEvent(dto);
			return Results.Created();
        }).WithTags("PerfumeWorns")
            .WithName("PostPerfumeWorn");

        app.MapDelete("/api/perfume-events/{id}", async (int id, PerfumeEventsRepo repo) => {
            await repo.DeletePerfumeEvent(id);
			return Results.NoContent();
        }).WithTags("PerfumeWorns")
            .WithName("DeletePerfumeWorn");
    }
}

