using PerfumeTracker.Server.Features.Perfumes.Services;
using Pgvector.EntityFrameworkCore;

namespace PerfumeTracker.Server.Features.WeeklyDigests;

public record DigestCandidate(Guid Id, string House, string PerfumeName, string Family, decimal Rating,
	decimal MlLeft, int TimesWorn, DateTime? LastWorn, List<string> Tags, string? LastComment);
public record DigestCandidatePage(int Total, int Offset, List<DigestCandidate> Perfumes);

public interface IDigestCandidates {
	Task<DigestCandidatePage> Get(RecommendationStrategy category, int offset, int count, string? search, CancellationToken ct);
	Task<object> GetContext(DateTime weekStart, CancellationToken ct);
}

public class DigestCandidates(PerfumeTrackerContext db) : IDigestCandidates {
	private static string Clip(string? value, int length) => (value ?? "")[..Math.Min(value?.Length ?? 0, length)];

	public async Task<object> GetContext(DateTime weekStart, CancellationToken ct) {
		var profile = await db.UserProfiles.AsNoTracking().FirstAsync(ct);
		var recent = await db.Perfumes.AsNoTracking().Where(p => p.LastWorn != null)
			.OrderByDescending(p => p.LastWorn).Take(5)
			.Select(p => new { p.Id, p.House, p.PerfumeName, p.LastWorn }).ToListAsync(ct);
		var previous = await db.WeeklyDigests.AsNoTracking().Where(d => d.Status == "Completed" && d.WeekStart < weekStart)
			.OrderByDescending(d => d.WeekStart).Take(4).Select(d => d.Payload).ToListAsync(ct);
		return new { weekStart, season = PerfumeRecommender.Season.ToString(), profile.MinimumRating,
			profile.DayFilter, recentWears = recent,
			previousDigests = previous.Select(p => JsonSerializer.Deserialize<DigestPayload>(p!, DigestJson.Options))
				.Select(p => new { p!.MoodOrOccasion, categories = p.Categories.Select(c => new { c.Strategy, perfumeIds = c.Picks.Select(p => p.PerfumeId) }) }) };
	}

	public async Task<DigestCandidatePage> Get(RecommendationStrategy category, int offset, int count, string? search, CancellationToken ct) {
		if (offset < 0 || count is < 1 or > 60 || (search?.Length ?? 0) > 120)
			throw new ArgumentException("Use offset >= 0, count 1-60, and search up to 120 characters.");
		var profile = await db.UserProfiles.AsNoTracking().FirstAsync(ct);
		var query = RecommendationQueries.Eligible(db, profile.MinimumRating, []).AsNoTracking();
		var total = await query.CountAsync(ct);
		IOrderedQueryable<Perfume> ordered;
		if (!string.IsNullOrWhiteSpace(search)) {
			var term = search.Trim().ToLowerInvariant();
			// Rank matches first rather than hiding the rest of the collection from the editor.
			ordered = query.OrderByDescending(p => p.House.ToLower().Contains(term) || p.PerfumeName.ToLower().Contains(term)
				|| p.Family.ToLower().Contains(term) || p.PerfumeTags.Any(t => t.Tag.TagName.ToLower().Contains(term)));
		} else {
			switch (category) {
				case RecommendationStrategy.ForgottenFavorite:
					ordered = query.OrderByDescending(p => p.LatestRating).ThenBy(p => p.LastWorn != null).ThenBy(p => p.LastWorn); break;
				case RecommendationStrategy.LeastUsed:
					ordered = query.OrderBy(p => p.WearCount).ThenBy(p => p.LastWorn != null).ThenBy(p => p.LastWorn); break;
				case RecommendationStrategy.LongestTimeAgo:
					ordered = query.OrderBy(p => p.LastWorn != null).ThenBy(p => p.LastWorn); break;
				case RecommendationStrategy.Seasonal:
					var words = SeasonalWords(PerfumeRecommender.Season);
					ordered = query.OrderByDescending(p => p.PerfumeTags.Count(t => words.Contains(t.Tag.TagName.ToLower()))); break;
				case RecommendationStrategy.SimilarToLastUsed:
					var vectors = await db.Perfumes.Where(p => p.LastWorn != null && p.PerfumeDocument != null && p.PerfumeDocument.Embedding != null)
						.OrderByDescending(p => p.LastWorn).Take(5).Select(p => p.PerfumeDocument!.Embedding!).ToListAsync(ct);
					if (vectors.Count == 0) { ordered = query.OrderByDescending(p => p.LatestRating); break; }
					var arrays = vectors.Select(v => v.ToArray()).ToList();
					var average = new Pgvector.Vector(Enumerable.Range(0, arrays[0].Length).Select(i => arrays.Average(a => a[i])).ToArray());
					ordered = query.OrderBy(p => p.PerfumeDocument == null || p.PerfumeDocument.Embedding == null)
						.ThenBy(p => p.PerfumeDocument!.Embedding!.L2Distance(average)); break;
				case RecommendationStrategy.Random:
					// Stable paging, with a different collection permutation each UTC week.
					var week = WeeklyDigestSchedule.LatestDueWeek(DateTime.UtcNow).Ticks / TimeSpan.TicksPerDay / 7;
					var segment = new[] { 0, 9, 14, 19, 24 }[(int)(week % 5)];
					ordered = query.OrderBy(p => p.Id.ToString().Substring(segment, 8)); break;
				default:
					ordered = query.OrderByDescending(p => p.LatestRating); break;
			}
		}
		var perfumes = await ordered.ThenBy(p => p.Id).Skip(offset).Take(count)
			.Select(p => new DigestCandidate(p.Id, p.House, p.PerfumeName, p.Family, p.LatestRating, p.MlLeft, p.WearCount,
				p.LastWorn, p.PerfumeTags.Select(t => t.Tag.TagName).ToList(),
				p.PerfumeRatings.OrderByDescending(e => e.RatingDate).Select(e => e.Comment).FirstOrDefault()))
			.ToListAsync(ct);
		return new(total, offset, perfumes.Select(p => p with { House = Clip(p.House, 160), PerfumeName = Clip(p.PerfumeName, 160),
			Family = Clip(p.Family, 120), Tags = p.Tags.Take(25).Select(t => Clip(t, 80)).ToList(), LastComment = Clip(p.LastComment, 400) }).ToList());
	}

	private static string[] SeasonalWords(PerfumeRecommender.Seasons season) => season switch {
		PerfumeRecommender.Seasons.Winter => ["warm", "spicy", "cozy", "amber", "vanilla", "gourmand", "oud", "tobacco", "leather"],
		PerfumeRecommender.Seasons.Spring => ["fresh", "floral", "green", "light", "citrus", "jasmine", "rose"],
		PerfumeRecommender.Seasons.Summer => ["citrus", "aquatic", "fresh", "light", "fruity", "marine", "coconut", "mint"],
		_ => ["woody", "spicy", "earthy", "amber", "patchouli", "sandalwood", "fig", "leather"]
	};
}
