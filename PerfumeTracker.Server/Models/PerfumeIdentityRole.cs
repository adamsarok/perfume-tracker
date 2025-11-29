namespace PerfumeTracker.Server.Models;

public class PerfumeIdentityRole : IdentityRole<Guid> {
	public PerfumeIdentityRole() : base() { }
	public PerfumeIdentityRole(string roleName) : base(roleName) { }
}