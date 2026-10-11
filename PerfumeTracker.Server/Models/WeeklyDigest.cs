namespace PerfumeTracker.Server.Models;

public class WeeklyDigest : UserEntity {
	public DateTime WeekStart { get; set; }
	public string Status { get; set; } = "Pending";
	public Guid? LeaseOwner { get; set; }
	public DateTime? LeaseUntil { get; set; }
	public int Attempts { get; set; }
	public long TokensUsed { get; set; }
	public string Model { get; set; } = "";
	public string Checkpoint { get; set; } = "{}";
	public string? Payload { get; set; }
	public string? LastError { get; set; }
	public DateTime? CompletedAt { get; set; }
}
