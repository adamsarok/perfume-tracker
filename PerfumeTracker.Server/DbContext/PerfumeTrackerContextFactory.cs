using Microsoft.EntityFrameworkCore.Design;
using PerfumeTracker.Server.Features.Auth;

namespace PerfumeTracker.Server.DbContext;

public class PerfumeTrackerContextFactory : IDesignTimeDbContextFactory<PerfumeTrackerContext> {
	public PerfumeTrackerContext CreateDbContext(string[] args) {
		var optionsBuilder = new DbContextOptionsBuilder<PerfumeTrackerContext>();

		// This connection string is only used for design-time operations (migrations, etc.)
		// It will be overridden at runtime by Program.cs
		var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__PerfumeTracker");

		optionsBuilder.UseNpgsql(connectionString, o => o.UseVector());

		// Use a null tenant provider for design-time operations
		return new PerfumeTrackerContext(optionsBuilder.Options, new DesignTimeTenantProvider());
	}
}

// Design-time tenant provider that returns null (no tenant filtering during migrations)
public class DesignTimeTenantProvider : ITenantProvider {
	public Guid? GetCurrentUserId() => null;
}
