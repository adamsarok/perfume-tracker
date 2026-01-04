namespace PerfumeTracker.Server.Models;

public partial class Tag : UserEntity {
	public string TagName { get; set; } = null!;
	public string? Color { get; set; } = null;
	public string? Description { get; set; } = null;
	public int BackfillAttempts { get; set; } = 0;
	public DateTime? LastBackfillAttempt { get; set; } = null;
	public virtual ICollection<PerfumeTag> PerfumeTags { get; set; } = new List<PerfumeTag>();
}
