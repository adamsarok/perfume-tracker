namespace PerfumeTracker.Server.Models;

public class Invite : Entity {
	public string Email { get; set; } = string.Empty;
	public bool IsUsed { get; set; }
}
