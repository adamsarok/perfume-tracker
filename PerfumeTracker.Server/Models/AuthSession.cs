namespace PerfumeTracker.Server.Models;

public class AuthSession {
	// SHA-256 hash of the session key handed to the client, never the key itself
	public required string Id { get; set; }
	// Serialized authentication ticket
	public required byte[] Ticket { get; set; }
	// Denormalized from the ticket so sessions can be revoked per user
	public Guid? UserId { get; set; }
	public string? UserAgent { get; set; }
	public string? IpAddress { get; set; }
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	// Idle expiry capped by the absolute lifetime, enforced server-side on retrieve
	public DateTime ExpiresAt { get; set; } = DateTime.UtcNow;
}
