
using Carter;

namespace FountainPensNg.Server.API {
    public class FountainPenModule : ICarterModule {
        public void AddRoutes(IEndpointRouteBuilder app) {
            //TODO placeholder

            //app.MapGet("/api/FountainPens", async (FountainPensRepo repo) =>
            //    await repo.GetFountainPens())
            //    .WithTags("FountainPens")
            //    .WithName("GetFountainPens");

            //app.MapGet("/api/FountainPens/{id}", async (int id, FountainPensRepo repo) => {
            //    var fountainPen = await repo.GetFountainPen(id);
            //    if (fountainPen == null) return Results.NotFound();
            //    return Results.Ok(fountainPen);
            //}).WithTags("FountainPens")
            //    .WithName("GetFountainPen");

            //app.MapPut("/api/FountainPens/{id}", async (int id, FountainPenUploadDTO dto, FountainPensRepo repo) => {
            //    var result = await repo.UpdateFountainPen(id, dto);
            //    return result.ResultType switch {
            //        ResultTypes.Ok => Results.Ok(result.FountainPen),
            //        ResultTypes.NotFound => Results.NotFound(),
            //        ResultTypes.BadRequest => Results.BadRequest(),
            //        _ => Results.InternalServerError()
            //    };
            //}).WithTags("FountainPens")
            //    .WithName("PutFountainPen");

            //app.MapPost("/api/FountainPens/", async (FountainPenUploadDTO dto, FountainPensRepo repo) => {
            //    var result = await repo.AddFountainPen(dto);
            //    return result.ResultType switch {
            //        ResultTypes.Ok => Results.CreatedAtRoute("GetFountainPen", new { id = result?.FountainPen?.Id }, result?.FountainPen),
            //        ResultTypes.NotFound => Results.NotFound(),
            //        ResultTypes.BadRequest => Results.BadRequest(),
            //        _ => Results.InternalServerError()
            //    };
            //}).WithTags("FountainPens")
            //    .WithName("PostFountainPen");

            //app.MapDelete("/api/FountainPens/{id}", async (int id, FountainPensRepo repo) => {
            //    var result = await repo.DeleteFountainPen(id);
            //    return result.ResultType switch {
            //        ResultTypes.Ok => Results.NoContent(),
            //        ResultTypes.NotFound => Results.NotFound(),
            //        _ => Results.InternalServerError()
            //    };
            //}).WithTags("FountainPens")
            //    .WithName("DeleteFountainPen");
        }
    }
}
