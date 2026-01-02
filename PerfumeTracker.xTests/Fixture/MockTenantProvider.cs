using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.xTests.Fixture;

public class MockTenantProvider : ITenantProvider {
	public Guid? MockTenantId { get; set; }
	public Guid? GetCurrentUserId() {
		return MockTenantId;
	}
}
