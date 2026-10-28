using System.Security.Claims;

namespace PerfumeTracker.Server.Features.Auth;

public interface ITenantProvider {
	Guid? GetCurrentUserId();
}

public class TenantProvider(IHttpContextAccessor accessor) : ITenantProvider {
	public Guid? GetCurrentUserId() {
		// Cookie validation can resolve this service before authentication sets HttpContext.User.
		var userId = accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
		return Guid.TryParse(userId, out var guid) ? guid : null;
	}
}
