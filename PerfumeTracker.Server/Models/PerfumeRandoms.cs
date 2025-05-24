namespace PerfumeTracker.Server.Models;
public partial class PerfumeRandoms : Entity {
	public Guid PerfumeId { get; set; }
	public virtual Perfume Perfume { get; set; } = null!;
	public bool IsAccepted { get; set; }
}
