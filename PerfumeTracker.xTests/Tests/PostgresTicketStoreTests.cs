using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.xTests.Fixture;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PerfumeTracker.xTests.Tests;

[CollectionDefinition("Postgres Ticket Store Tests")]
public class PostgresTicketStoreCollection : ICollectionFixture<PostgresTicketStoreFixture>;

public class PostgresTicketStoreFixture : DbFixture {
	public PostgresTicketStoreFixture() : base() { }

	public async override Task SeedTestData(PerfumeTrackerContext context) {
		await context.Database.ExecuteSqlRawAsync("truncate table \"public\".\"AuthSession\" cascade; ");
	}
}

[Collection("Postgres Ticket Store Tests")]
public class PostgresTicketStoreTests(PostgresTicketStoreFixture fixture) {
	private PostgresTicketStore Store => fixture.Factory.Services.GetRequiredService<PostgresTicketStore>();

	[Fact]
	public async Task StoreAsync_PersistsHashedKeyAndRequestContext() {
		var userId = Guid.NewGuid();
		var httpContext = new DefaultHttpContext();
		httpContext.Request.Headers.UserAgent = "xunit-test-agent/1.0";
		httpContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.7");

		var key = await Store.StoreAsync(CreateTicket(userId), httpContext, TestContext.Current.CancellationToken);

		var session = await FindSessionByRawKey(key);
		Assert.NotNull(session);
		// The stored id is the SHA-256 hash of the key, never the key itself
		Assert.Equal(HashSessionKey(key), session.Id);
		Assert.DoesNotContain(key, session.Id);
		Assert.Equal(userId, session.UserId);
		Assert.Equal("xunit-test-agent/1.0", session.UserAgent);
		Assert.Equal("203.0.113.7", session.IpAddress);
	}

	[Fact]
	public async Task StoreAsync_WithoutHttpContext_LeavesContextColumnsNull() {
		var key = await Store.StoreAsync(CreateTicket(), TestContext.Current.CancellationToken);

		var session = await FindSessionByRawKey(key);
		Assert.NotNull(session);
		Assert.Null(session.UserAgent);
		Assert.Null(session.IpAddress);
	}

	[Fact]
	public async Task RetrieveAsync_RoundTripsTheTicket() {
		var userId = Guid.NewGuid();
		var key = await Store.StoreAsync(CreateTicket(userId), TestContext.Current.CancellationToken);

		var ticket = await Store.RetrieveAsync(key, TestContext.Current.CancellationToken);

		Assert.NotNull(ticket);
		Assert.Equal(IdentityConstants.ApplicationScheme, ticket.AuthenticationScheme);
		Assert.Equal(userId.ToString(), ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
	}

	[Fact]
	public async Task RetrieveAsync_UnknownKey_ReturnsNull() {
		var ticket = await Store.RetrieveAsync("no-such-session-key", TestContext.Current.CancellationToken);
		Assert.Null(ticket);
	}

	[Fact]
	public async Task RetrieveAsync_IdleExpired_ReturnsNullAndDeletesRow() {
		var key = await Store.StoreAsync(CreateTicket(), TestContext.Current.CancellationToken);
		await UpdateSession(key, s => s.ExpiresAt = DateTime.UtcNow.AddMinutes(-1));

		var ticket = await Store.RetrieveAsync(key, TestContext.Current.CancellationToken);
		Assert.Null(ticket);
		Assert.Null(await FindSessionByRawKey(key));
	}

	[Fact]
	public async Task RetrieveAsync_AbsoluteLifetimeExceeded_ReturnsNull() {
		var key = await Store.StoreAsync(CreateTicket(idleLifetime: TimeSpan.FromHours(1)), TestContext.Current.CancellationToken);
		// Idle expiry is still in the future, but the session was created beyond the absolute lifetime
		await UpdateSession(key, s => s.CreatedAt = DateTime.UtcNow.AddHours(-(TestAbsoluteLifetimeHours + 1)));

		var ticket = await Store.RetrieveAsync(key, TestContext.Current.CancellationToken);
		Assert.Null(ticket);
	}

	[Fact]
	public async Task RenewAsync_UpdatesTicketForSameKey() {
		var key = await Store.StoreAsync(CreateTicket(idleLifetime: TimeSpan.FromHours(1)), TestContext.Current.CancellationToken);
		var renewedUserId = Guid.NewGuid();

		await Store.RenewAsync(key, CreateTicket(renewedUserId, idleLifetime: TimeSpan.FromHours(2)), TestContext.Current.CancellationToken);

		var ticket = await Store.RetrieveAsync(key, TestContext.Current.CancellationToken);
		Assert.NotNull(ticket);
		Assert.Equal(renewedUserId.ToString(), ticket.Principal.FindFirstValue(ClaimTypes.NameIdentifier));
		var session = await FindSessionByRawKey(key);
		Assert.NotNull(session);
		Assert.True(session.ExpiresAt > DateTime.UtcNow.AddHours(1));
	}

	[Fact]
	public async Task RenewAsync_MissingSession_DoesNotResurrect() {
		const string key = "revoked-or-expired-key";

		await Store.RenewAsync(key, CreateTicket(), TestContext.Current.CancellationToken);

		Assert.Null(await FindSessionByRawKey(key));
	}

	[Fact]
	public async Task RemoveAsync_DeletesSession() {
		var key = await Store.StoreAsync(CreateTicket(), TestContext.Current.CancellationToken);

		await Store.RemoveAsync(key, TestContext.Current.CancellationToken);

		Assert.Null(await FindSessionByRawKey(key));
	}

	[Fact]
	public async Task StoreAsync_DeletesAlreadyExpiredSessions() {
		await using (var scope = fixture.Factory.Services.CreateAsyncScope()) {
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			db.AuthSessions.Add(new AuthSession {
				Id = "deadbeef".PadLeft(64, '0'),
				Ticket = [1, 2, 3],
				CreatedAt = DateTime.UtcNow.AddHours(-2),
				ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
			});
			await db.SaveChangesAsync(TestContext.Current.CancellationToken);
		}

		await Store.StoreAsync(CreateTicket(), TestContext.Current.CancellationToken);

		await using (var scope = fixture.Factory.Services.CreateAsyncScope()) {
			var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
			Assert.False(await db.AuthSessions.AnyAsync(s => s.Id == "deadbeef".PadLeft(64, '0'), TestContext.Current.CancellationToken));
		}
	}

	// Must match appsettings.Test.json Session:AbsoluteLifetimeHours
	private const int TestAbsoluteLifetimeHours = 336;

	private static AuthenticationTicket CreateTicket(Guid? userId = null, TimeSpan? idleLifetime = null) {
		var identity = new ClaimsIdentity(IdentityConstants.ApplicationScheme);
		if (userId is { } id) identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, id.ToString()));
		identity.AddClaim(new Claim(ClaimTypes.Name, "ticket-store-tester"));
		var properties = new AuthenticationProperties { ExpiresUtc = DateTimeOffset.UtcNow.Add(idleLifetime ?? TimeSpan.FromHours(24)) };
		return new AuthenticationTicket(new ClaimsPrincipal(identity), properties, IdentityConstants.ApplicationScheme);
	}

	private async Task<AuthSession?> FindSessionByRawKey(string key) {
		var sessionId = HashSessionKey(key);
		await using var scope = fixture.Factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		return await db.AuthSessions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == sessionId, TestContext.Current.CancellationToken);
	}

	private async Task UpdateSession(string key, Action<AuthSession> update) {
		var sessionId = HashSessionKey(key);
		await using var scope = fixture.Factory.Services.CreateAsyncScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var session = await db.AuthSessions.FirstAsync(s => s.Id == sessionId, TestContext.Current.CancellationToken);
		update(session);
		await db.SaveChangesAsync(TestContext.Current.CancellationToken);
	}

	// Mirrors PostgresTicketStore.HashSessionKey
	private static string HashSessionKey(string key) => Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
}
