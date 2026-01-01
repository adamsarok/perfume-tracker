namespace PerfumeTracker.Server.Models;

public partial class GlobalTag : Entity {
	public string TagName { get; set; } = null!;
	public string Color { get; set; } = null!;
	public virtual ICollection<GlobalPerfumeTag> GlobalPerfumeTags { get; set; } = new List<GlobalPerfumeTag>();
}
