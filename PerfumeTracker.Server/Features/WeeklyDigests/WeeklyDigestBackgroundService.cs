using Microsoft.Extensions.Options;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Startup;

namespace PerfumeTracker.Server.Features.WeeklyDigests;

public static class WeeklyDigestSchedule {
	public static DateTime LatestDueWeek(DateTime utcNow) {
		var sunday = utcNow.Date.AddDays(-(int)utcNow.DayOfWeek);
		return DateTime.SpecifyKind(utcNow < sunday.AddHours(6) ? sunday.AddDays(-7) : sunday, DateTimeKind.Utc);
	}
}

public class WeeklyDigestBackgroundService(IServiceScopeFactory scopes, IOptions<WeeklyDigestOptions> options,
	IOptions<OpenAIOptions> openAi, ILogger<WeeklyDigestBackgroundService> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		while (!stoppingToken.IsCancellationRequested) {
			try { await ProcessDue(stoppingToken); }
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
			catch (Exception ex) { logger.LogError(ex, "Weekly digest scheduler failed."); }
			try { await Task.Delay(TimeSpan.FromHours(1), stoppingToken); }
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
		}
	}

	public async Task ProcessDue(CancellationToken ct) {
		using var discovery = scopes.CreateScope();
		var db = discovery.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var users = await db.Users.AsNoTracking().Where(u => u.WeeklyDigestEnabled).Select(u => u.Id).ToListAsync(ct);
		var week = WeeklyDigestSchedule.LatestDueWeek(DateTime.UtcNow);
		foreach (var userId in users) {
			ct.ThrowIfCancellationRequested();
			try { await GenerateForUser(userId, week, ct); }
			catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
			catch (Exception ex) { logger.LogError(ex, "Weekly digest failed for user {UserId}, week {Week}", userId, week); }
		}
	}

	public async Task GenerateForUser(Guid userId, DateTime week, CancellationToken stoppingToken) {
		using var activity = Diagnostics.ActivitySource.StartActivity("weekly_digest.generate");
		activity?.SetTag("job.name", nameof(WeeklyDigestBackgroundService));
		activity?.SetTag("digest.week", week);
		using var scope = scopes.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		db.TenantProvider = new DigestTenant(userId);
		if (!await db.Users.AnyAsync(u => u.Id == userId && u.WeeklyDigestEnabled, stoppingToken)) return;
		var cfg = options.Value;
		var modelName = cfg.Model ?? openAi.Value.AssistantModel;
		var id = Guid.NewGuid();
		var now = DateTime.UtcNow;
		var emptyCheckpoint = "{}";
		await db.Database.ExecuteSqlInterpolatedAsync($"""
			INSERT INTO "WeeklyDigest" ("Id", "UserId", "WeekStart", "Status", "Attempts", "TokensUsed", "Model", "Checkpoint", "CreatedAt", "UpdatedAt", "IsDeleted")
			VALUES ({id}, {userId}, {week}, 'Pending', 0, 0, {modelName}, {emptyCheckpoint}, {now}, {now}, FALSE)
			ON CONFLICT ("UserId", "WeekStart") DO NOTHING
			""", stoppingToken);
		var owner = Guid.NewGuid();
		var exhausted = await db.WeeklyDigests.Where(d => d.WeekStart == week
			&& (d.Status == "Pending" || (d.Status == "Running" && d.LeaseUntil < now))
			&& (d.Attempts >= cfg.MaxAttempts || d.TokensUsed >= cfg.MaxTokens))
			.ExecuteUpdateAsync(s => s.SetProperty(d => d.Status, "Failed").SetProperty(d => d.LastError, "Weekly attempts or token budget exhausted."), stoppingToken);
		if (exhausted > 0) {
			var failed = await db.WeeklyDigests.AsNoTracking().SingleAsync(d => d.WeekStart == week, stoppingToken);
			logger.LogWarning("Weekly digest run finished early due to {LimitReason}. Digest {DigestId}, user {UserId}, week {Week}, attempts {Attempts}, tokens used {TokensUsed}/{MaxTokens}",
				failed.TokensUsed >= cfg.MaxTokens ? "token limit" : "attempt limit", failed.Id, userId, week, failed.Attempts, failed.TokensUsed, cfg.MaxTokens);
		}
		var claimed = await db.WeeklyDigests.Where(d => d.WeekStart == week && d.Attempts < cfg.MaxAttempts && d.TokensUsed < cfg.MaxTokens
			&& (d.Status == "Pending" || (d.Status == "Running" && d.LeaseUntil < now)))
			.ExecuteUpdateAsync(s => s.SetProperty(d => d.Status, "Running").SetProperty(d => d.LeaseOwner, (Guid?)owner)
				.SetProperty(d => d.LeaseUntil, (DateTime?)now.AddMinutes(2)).SetProperty(d => d.Attempts, d => d.Attempts + 1), stoppingToken);
		if (claimed == 0) return;
		var run = await db.WeeklyDigests.AsNoTracking().SingleAsync(d => d.WeekStart == week, stoppingToken);
		logger.LogInformation("Weekly digest processing started. Digest {DigestId}, user {UserId}, week {Week}, attempt {Attempt}, model {Model}, tokens used {TokensUsed}/{MaxTokens}",
			run.Id, userId, week, run.Attempts, run.Model, run.TokensUsed, cfg.MaxTokens);
		using var work = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
		await using var renewal = new DigestLeaseRenewal(run.Id, ct => RenewLease(run.Id, owner, ct), work, stoppingToken, logger);
		try {
			if (!await db.UserProfiles.AnyAsync(work.Token)
				|| !await db.Perfumes.AnyAsync(p => p.MlLeft > 0 && p.LatestRating >= db.UserProfiles.Select(p => p.MinimumRating).First(), work.Token)) {
				run.Status = "Skipped";
			} else {
				var agent = scope.ServiceProvider.GetRequiredService<WeeklyDigestAgent>();
				var payload = await agent.Run(run, Save, work.Token);
				var ids = payload.Categories.SelectMany(c => c.Picks).Select(p => p.PerfumeId).Distinct().ToList();
				var minimumRating = await db.UserProfiles.Select(p => p.MinimumRating).FirstAsync(work.Token);
				var validCount = await db.Perfumes.CountAsync(p => ids.Contains(p.Id) && p.MlLeft > 0 && p.LatestRating >= minimumRating, work.Token);
				if (validCount != ids.Count) throw new InvalidDataException("Selected perfumes changed eligibility during generation.");
				if (!await db.Users.AnyAsync(u => u.Id == userId && u.WeeklyDigestEnabled, work.Token)) run.Status = "Skipped";
				else {
					run.Payload = JsonSerializer.Serialize(payload, DigestJson.Options);
					run.Status = "Completed";
					run.CompletedAt = DateTime.UtcNow;
					run.LastError = null;
				}
			}
			await Save(work.Token);
			logger.LogInformation("Weekly digest {Status} for {UserId}, week {Week}; {Tokens} tokens", run.Status, userId, week, run.TokensUsed);
		} catch (OperationCanceledException) when (work.IsCancellationRequested) {
			// Keep the checkpoint and lease expiry for safe restart recovery.
			if (stoppingToken.IsCancellationRequested) throw;
			logger.LogWarning("Weekly digest lease lost for {UserId}, week {Week}", userId, week);
		} catch (Exception ex) {
			var transient = ex is HttpRequestException or TimeoutException or TaskCanceledException
				|| ex is DigestApiException api && api.IsTransient;
			run.Status = transient && run.Attempts < cfg.MaxAttempts && run.TokensUsed < cfg.MaxTokens ? "Pending" : "Failed";
			run.LastError = ex.Message[..Math.Min(ex.Message.Length, 1000)];
			await Save(work.Token);
			logger.LogError(ex, "Weekly digest {Status} for {UserId}, tokens {Tokens}", run.Status, userId, run.TokensUsed);
		}
		async Task Save(CancellationToken ct) {
			var timestamp = DateTime.UtcNow;
			var released = run.Status != "Running";
			// Stop renewal before releasing ownership so a completion cannot be mistaken for lease loss.
			if (released) await renewal.StopAsync();
			var updated = await db.WeeklyDigests.Where(d => d.Id == run.Id && d.LeaseOwner == owner && d.LeaseUntil > timestamp)
				.ExecuteUpdateAsync(s => s.SetProperty(d => d.Checkpoint, run.Checkpoint).SetProperty(d => d.TokensUsed, run.TokensUsed)
					.SetProperty(d => d.Status, run.Status).SetProperty(d => d.Payload, run.Payload).SetProperty(d => d.LastError, run.LastError)
					.SetProperty(d => d.CompletedAt, run.CompletedAt).SetProperty(d => d.UpdatedAt, timestamp)
					.SetProperty(d => d.LeaseOwner, released ? null : (Guid?)owner)
					.SetProperty(d => d.LeaseUntil, released ? null : (DateTime?)timestamp.AddMinutes(2)), ct);
			if (updated != 1) { await work.CancelAsync(); throw new OperationCanceledException("Digest lease lost.", work.Token); }
		}
	}

	private async Task<bool> RenewLease(Guid id, Guid owner, CancellationToken ct) {
		using var scope = scopes.CreateScope();
		var db = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var now = DateTime.UtcNow;
		var updated = await db.WeeklyDigests.IgnoreQueryFilters().Where(d => d.Id == id && d.LeaseOwner == owner && d.Status == "Running" && d.LeaseUntil > now)
			.ExecuteUpdateAsync(s => s.SetProperty(d => d.LeaseUntil, (DateTime?)now.AddMinutes(2)), ct);
		return updated == 1;
	}

	private sealed class DigestTenant(Guid id) : ITenantProvider { public Guid? GetCurrentUserId() => id; }
}
