using System.Security.Claims;

namespace PerfumeTracker.Server.Features.Auth;

public interface ITenantProvider {
	Guid? GetCurrentUserId();
}

public class TenantProvider : ITenantProvider {
	private readonly Guid? _userId;

	public TenantProvider(IHttpContextAccessor accessor) {
		var userIdValue = accessor.HttpContext?.User.FindFirstValue("X-User-Id");
		_userId = Guid.TryParse(userIdValue, out var guid) ? guid : null;
	}

	public Guid? GetCurrentUserId() => _userId;
}