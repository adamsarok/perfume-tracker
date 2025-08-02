using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.xTests;
public class MockTenantProvider : ITenantProvider {
	public Guid? MockTenantId { get; set; }
	public Guid? GetCurrentUserId() {
		return MockTenantId;
	}
}
