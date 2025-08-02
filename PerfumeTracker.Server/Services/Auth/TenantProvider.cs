using System.Security.Claims;

namespace PerfumeTracker.Server.Services.Auth;

public interface ITenantProvider {
	Guid? GetCurrentUserId();
}

public class TenantProvider : ITenantProvider {
	private readonly Guid? _userId;

	public TenantProvider(IHttpContextAccessor accessor) {
		var userIdClaim = accessor.HttpContext?.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);
		if (userIdClaim != null) _userId = Guid.TryParse(userIdClaim.Value, out var guid) ? guid : null;
	}

	public Guid? GetCurrentUserId() => _userId;
}