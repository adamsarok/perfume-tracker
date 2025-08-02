using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PerfumeTracker.Server.Services.Common;

public class EntityInterceptor : SaveChangesInterceptor {
	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
		if (eventData.Context is PerfumeTrackerContext context) UpdateEntities(context);
		return base.SavingChanges(eventData, result);
	}
	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
		if (eventData.Context is PerfumeTrackerContext context) UpdateEntities(context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}
	private void UpdateEntities(PerfumeTrackerContext? context) {
		if (context == null) return;
		foreach (var entry in context.ChangeTracker.Entries<IEntity>()) {
			if (entry.State == EntityState.Added) {
				entry.Entity.CreatedAt = DateTime.UtcNow;
				if (entry.Entity is IUserEntity userEntity && userEntity.UserId == Guid.Empty) 					userEntity.UserId = context.TenantProvider.GetCurrentUserId() ?? throw new InvalidOperationException("Tenant/userID not set");
			}
			if (entry.State == EntityState.Added || entry.State == EntityState.Modified) 				entry.Entity.UpdatedAt = DateTime.UtcNow;
		}
	}
}
