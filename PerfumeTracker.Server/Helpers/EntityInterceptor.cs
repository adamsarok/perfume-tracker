﻿using Microsoft.EntityFrameworkCore.Diagnostics;

namespace PerfumeTracker.Server.Helpers;

public class EntityInterceptor : SaveChangesInterceptor {
	public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result) {
		UpdateEntities(eventData.Context);
		return base.SavingChanges(eventData, result);
	}
	public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default) {
		UpdateEntities(eventData.Context);
		return base.SavingChangesAsync(eventData, result, cancellationToken);
	}
	private void UpdateEntities(DbContext? context) {
		if (context == null) return;
		foreach (var entry in context.ChangeTracker.Entries<IEntity>()) {
			if (entry.State == EntityState.Added) {
				entry.Entity.CreatedAt = DateTime.UtcNow;
				entry.Entity.UserId = PerfumeTrackerContext.DEFAULT_USERID;
			}
			if (entry.State == EntityState.Added || entry.State == EntityState.Modified) {
				entry.Entity.UpdatedAt = DateTime.UtcNow;
			}
		}
	}
}
