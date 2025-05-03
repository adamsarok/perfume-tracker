namespace PerfumeTracker.Server.Models;

public interface IEntity {
	public DateTime Created_At { get; set; }
	public DateTime Updated_At { get; set; }
}
