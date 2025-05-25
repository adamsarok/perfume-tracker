﻿namespace PerfumeTracker.Server.Models;

public class Entity : IEntity {
	public Guid Id { get; set; } = Guid.NewGuid();
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
	public bool IsDeleted { get; set; }
	public Guid UserId { get; set; } = PerfumeTrackerContext.DefaultUserID;
}
