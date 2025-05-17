namespace PerfumeTracker.Server.Models;

public class Achievement : Entity {
	public int Id { get; set; }
	public string Name { get; set; } = null!;
	public string Description { get; set; } = null!;
	public int? MinPerfumesAdded { get; set; }
	public int? MinPerfumeWornDays { get; set; }
	public int? MinTags { get; set; }
	public int? MinPerfumeTags { get; set; }
	public int? MinStreak { get; set; }
	public int? MinXP { get; set; }
}
