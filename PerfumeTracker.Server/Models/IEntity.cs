namespace PerfumeTracker.Server.Models;

public interface IEntity {
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public bool IsDeleted { get; set; }
	public string UserId { get; set; }
}