namespace PerfumeTracker.Server.Models;

public class Invite : Entity {
	public string Email { get; set; }
	public bool IsUsed { get; set; }
}
