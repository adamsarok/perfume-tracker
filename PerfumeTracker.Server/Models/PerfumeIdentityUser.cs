namespace PerfumeTracker.Server.Models;

using Microsoft.AspNetCore.Identity;
public class PerfumeIdentityUser : IdentityUser<Guid> {
	// Operational setting; never accepted through user profile/settings endpoints.
	[System.Text.Json.Serialization.JsonIgnore]
	public bool WeeklyDigestEnabled { get; set; }
}
