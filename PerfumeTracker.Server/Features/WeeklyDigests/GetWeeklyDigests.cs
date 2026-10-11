using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.Server.Features.WeeklyDigests;

public record DigestSummary(Guid Id, DateTime WeekStart, DateTime? CompletedAt);
public record DigestDetail(Guid Id, DateTime WeekStart, DateTime? CompletedAt, DigestPayload Digest);

public class GetWeeklyDigests : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/weekly-digests", async (PerfumeTrackerContext db, CancellationToken ct, int page = 1) => {
			if (page < 1 || page > 10_000) return Results.BadRequest("Page must be between 1 and 10000.");
			var query = db.WeeklyDigests.AsNoTracking().Where(d => d.Status == "Completed");
			var total = await query.CountAsync(ct);
			var items = await query.OrderByDescending(d => d.WeekStart).Skip((page - 1) * 20).Take(20)
				.Select(d => new DigestSummary(d.Id, d.WeekStart, d.CompletedAt)).ToListAsync(ct);
			return Results.Ok(new { items, total, page });
		}).RequireAuthorization(Policies.READ).WithTags("WeeklyDigests");
		app.MapGet("/api/weekly-digests/{id:guid}", async (Guid id, PerfumeTrackerContext db, CancellationToken ct) => {
			var row = await db.WeeklyDigests.AsNoTracking().SingleOrDefaultAsync(d => d.Id == id && d.Status == "Completed", ct);
			if (row == null) return Results.NotFound();
			var payload = JsonSerializer.Deserialize<DigestPayload>(row.Payload!, DigestJson.Options)!;
			var ids = payload.Categories.SelectMany(c => c.Picks).Select(p => p.PerfumeId).Distinct().ToList();
			var existing = (await db.Perfumes.AsNoTracking().Where(p => ids.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct)).ToHashSet();
			payload = payload with { Categories = payload.Categories.Select(c => c with {
				Picks = c.Picks.Select(p => p with { IsAvailable = existing.Contains(p.PerfumeId) }).ToList()
			}).ToList() };
			return Results.Ok(new DigestDetail(row.Id, row.WeekStart, row.CompletedAt, payload));
		}).RequireAuthorization(Policies.READ).WithTags("WeeklyDigests");
	}
}
