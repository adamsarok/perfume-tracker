namespace PerfumeTracker.Server.Models;
public class PerfumeRandoms : Entity {
	public Guid PerfumeId { get; set; }
	public virtual Perfume Perfume { get; set; } = null!;
	public bool IsAccepted { get; set; }
}
