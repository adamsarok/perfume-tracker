namespace PerfumeTracker.Server.Models;

public partial class PerfumeTag : Entity {
	public Guid PerfumeId { get; set; }
	public Guid TagId { get; set; }
	public virtual Perfume Perfume { get; set; } = null!;
	public virtual Tag Tag { get; set; } = null!;
}
