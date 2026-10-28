using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PerfumeTracker.Server.Options;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PerfumeTracker.Server.Features.Auth;

// Stores authentication tickets server-side; the client cookie only carries a random key.
// Registered as a singleton, so a scoped PerfumeTrackerContext is created per operation.
public class PostgresTicketStore(IServiceScopeFactory scopeFactory, IOptions<SessionConfiguration> sessionOptions,
	ILogger<PostgresTicketStore> logger) : ITicketStore {
	private const int UserAgentMaxLength = 512;
	private const int IpAddressMaxLength = 45;

	private readonly SessionConfiguration sessionConfiguration = sessionOptions.Value;

	public Task<string> StoreAsync(AuthenticationTicket ticket) => StoreCoreAsync(ticket, null, CancellationToken.None);

	public Task<string> StoreAsync(AuthenticationTicket ticket, CancellationToken cancellationToken) => StoreCoreAsync(ticket, null, cancellationToken);

	// Overridden so the session row records where the login came from.
	public Task<string> StoreAsync(AuthenticationTicket ticket, HttpContext httpContext, CancellationToken cancellationToken) => StoreCoreAsync(ticket, httpContext, cancellationToken);

	public Task RenewAsync(string key, AuthenticationTicket ticket) => RenewCoreAsync(key, ticket, CancellationToken.None);

	public Task RenewAsync(string key, AuthenticationTicket ticket, CancellationToken cancellationToken) => RenewCoreAsync(key, ticket, cancellationToken);

	private async Task RenewCoreAsync(string key, AuthenticationTicket ticket, CancellationToken cancellationToken) {
		var sessionId = HashSessionKey(key);

		await using var scope = scopeFactory.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var session = await db.AuthSessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
		if (session is null) {
			// Session was revoked or expired; do not resurrect it.
			logger.LogWarning("Cannot renew missing auth session {SessionId}.", sessionId);
			return;
		}

		if (ticket.AuthenticationScheme != IdentityConstants.ApplicationScheme) {
			logger.LogWarning("Renewed auth session {SessionId} with unexpected scheme {Scheme}.", sessionId, ticket.AuthenticationScheme);
		}

		session.Ticket = TicketSerializer.Default.Serialize(ticket);
		session.UserId = GetUserId(ticket.Principal);
		session.ExpiresAt = ComputeExpiry(ticket, session.CreatedAt);
		await db.SaveChangesAsync(cancellationToken);
	}

	public Task<AuthenticationTicket?> RetrieveAsync(string key) => RetrieveCoreAsync(key, CancellationToken.None);

	public Task<AuthenticationTicket?> RetrieveAsync(string key, CancellationToken cancellationToken) => RetrieveCoreAsync(key, cancellationToken);

	private async Task<AuthenticationTicket?> RetrieveCoreAsync(string key, CancellationToken cancellationToken) {
		var sessionId = HashSessionKey(key);

		await using var scope = scopeFactory.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		var session = await db.AuthSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
		if (session is null) return null;

		if (IsExpired(session, out var reason)) {
			logger.LogInformation("Auth session {SessionId} rejected: {Reason}.", sessionId, reason);
			await db.AuthSessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync(cancellationToken);
			return null;
		}

		var ticket = TicketSerializer.Default.Deserialize(session.Ticket);
		if (ticket is null) {
			logger.LogWarning("Auth session {SessionId} could not be deserialized; removing.", sessionId);
			await db.AuthSessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync(cancellationToken);
			return null;
		}
		return ticket;
	}

	public Task RemoveAsync(string key) => RemoveCoreAsync(key, CancellationToken.None);

	public Task RemoveAsync(string key, CancellationToken cancellationToken) => RemoveCoreAsync(key, cancellationToken);

	private async Task RemoveCoreAsync(string key, CancellationToken cancellationToken) {
		var sessionId = HashSessionKey(key);

		await using var scope = scopeFactory.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		await db.AuthSessions.Where(s => s.Id == sessionId).ExecuteDeleteAsync(cancellationToken);
	}

	private async Task<string> StoreCoreAsync(AuthenticationTicket ticket, HttpContext? httpContext, CancellationToken cancellationToken) {
		if (ticket.AuthenticationScheme != IdentityConstants.ApplicationScheme) {
			logger.LogWarning("Storing auth session with unexpected scheme {Scheme}.", ticket.AuthenticationScheme);
		}

		var now = DateTime.UtcNow;
		var (key, sessionId) = CreateSessionKey();

		await using var scope = scopeFactory.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();

		// Opportunistic cleanup; logins are rare, so this keeps the table small without a scheduler.
		await db.AuthSessions.Where(s => s.ExpiresAt < now).ExecuteDeleteAsync(cancellationToken);

		db.AuthSessions.Add(new Models.AuthSession {
			Id = sessionId,
			Ticket = TicketSerializer.Default.Serialize(ticket),
			UserId = GetUserId(ticket.Principal),
			UserAgent = Truncate(httpContext?.Request.Headers.UserAgent.ToString(), UserAgentMaxLength),
			IpAddress = Truncate(httpContext?.Connection.RemoteIpAddress?.ToString(), IpAddressMaxLength),
			CreatedAt = now,
			ExpiresAt = ComputeExpiry(ticket, now),
		});
		await db.SaveChangesAsync(cancellationToken);

		return key;
	}

	private DateTime ComputeExpiry(AuthenticationTicket ticket, DateTime createdAt) {
		var absoluteExpiry = createdAt + sessionConfiguration.AbsoluteLifetime;
		var idleExpiry = ticket.Properties.ExpiresUtc is { } expiresUtc
			? expiresUtc.UtcDateTime
			: createdAt + sessionConfiguration.IdleTimeout;
		return idleExpiry < absoluteExpiry ? idleExpiry : absoluteExpiry;
	}

	private bool IsExpired(Models.AuthSession session, out string reason) {
		var now = DateTime.UtcNow;
		if (session.ExpiresAt < now) {
			reason = "idle timeout";
			return true;
		}
		if (session.CreatedAt + sessionConfiguration.AbsoluteLifetime < now) {
			reason = "absolute lifetime exceeded";
			return true;
		}
		reason = string.Empty;
		return false;
	}

	private static (string Key, string SessionId) CreateSessionKey() {
		// The raw key goes to the client, only its hash is persisted.
		var key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
		return (key, HashSessionKey(key));
	}

	// The cookie round-trips the key verbatim, so hashing the string is enough;
	// no need to decode it back to raw bytes.
	private static string HashSessionKey(string key) {
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
		return Convert.ToHexStringLower(hash);
	}

	private static Guid? GetUserId(ClaimsPrincipal principal) {
		return Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : null;
	}

	private static string? Truncate(string? value, int maxLength) {
		return value is { Length: > 0 } && value.Length > maxLength ? value[..maxLength] : value;
	}
}
