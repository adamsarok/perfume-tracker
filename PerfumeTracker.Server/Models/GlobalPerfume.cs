namespace PerfumeTracker.Server.Models;

public partial class GlobalPerfume : Entity {
	public string House { get; set; } = null!;
	public string PerfumeName { get; set; } = null!;
	public string Family { get; set; } = null!;
	public Guid? ImageObjectKeyNew { get; set; } = null!;
	public NpgsqlTsVector FullText { get; set; } = null!;
	public virtual ICollection<GlobalPerfumeTag> GlobalPerfumeTags { get; } = [];
}