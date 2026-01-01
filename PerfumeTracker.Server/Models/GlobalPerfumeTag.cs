namespace PerfumeTracker.Server.Models;

public partial class GlobalPerfumeTag : Entity {
	public Guid PerfumeId { get; set; }
	public Guid TagId { get; set; }
	public virtual GlobalPerfume GlobalPerfume { get; set; } = null!;
	public virtual GlobalTag GlobalTag { get; set; } = null!;
}
