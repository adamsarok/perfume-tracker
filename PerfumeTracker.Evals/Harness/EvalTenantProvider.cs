using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.Evals.Harness;

public sealed class EvalTenantProvider : ITenantProvider {
	public Guid? CurrentUserId { get; set; }
	public Guid? GetCurrentUserId() => CurrentUserId;
}
