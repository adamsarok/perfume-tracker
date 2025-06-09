namespace PerfumeTracker.Server.Models;

public interface IEntity {
	public Guid Id { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
	public bool IsDeleted { get; set; }
}
public interface IUserEntity : IEntity {
	public Guid UserId { get; set; }
}