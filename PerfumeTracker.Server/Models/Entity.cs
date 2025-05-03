namespace PerfumeTracker.Server.Models;

public class Entity : IEntity {
	public DateTime Created_At { get; set; } = DateTime.UtcNow;
	public DateTime Updated_At { get; set; } = DateTime.UtcNow;
}
