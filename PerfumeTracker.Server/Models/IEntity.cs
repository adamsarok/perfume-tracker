namespace PerfumeTracker.Server.Models;

public interface IEntity {
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}
