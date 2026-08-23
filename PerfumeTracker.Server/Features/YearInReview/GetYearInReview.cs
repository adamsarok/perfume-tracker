using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Common;

namespace PerfumeTracker.Server.Features.YearInReview;

public record YearInReviewRankedItem(
	string Name,
	string? Subtitle,
	int Count,
	string? Color = null,
	string? ImageUrl = null
);

public record YearInReviewCategory(
	string Key,
	string Title,
	string? PerfumeName,
	string? House,
	string? ImageUrl,
	string Detail,
	decimal? RatingFrom = null,
	decimal? RatingTo = null,
	int? GapDays = null,
	string? Note = null
);

public record YearInReviewResponse(
	int Year,
	int TotalWears,
	int UniquePerfumes,
	int UniqueHouses,
	int ActiveDays,
	IReadOnlyList<YearInReviewRankedItem> TopPerfumes,
	IReadOnlyList<YearInReviewRankedItem> TopHouses,
	IReadOnlyList<YearInReviewRankedItem> TopTags,
	YearInReviewRankedItem? BusiestMonth,
	YearInReviewRankedItem? BusiestDay,
	IReadOnlyList<YearInReviewCategory> Categories
);

public record GetYearInReviewQuery(int Year) : IQuery<YearInReviewResponse>;

public class GetYearInReviewEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/year-in-review/{year:int}", async (
			int year,
			ISender sender,
			CancellationToken cancellationToken) =>
			await sender.Send(new GetYearInReviewQuery(year), cancellationToken))
			.WithTags("YearInReview")
			.WithName("GetYearInReview")
			.RequireAuthorization(Policies.READ);
	}
}

public class GetYearInReviewHandler(
	PerfumeTrackerContext context,
	IPresignedUrlService presignedUrlService,
	IYearInReviewAi? yearInReviewAi = null)
	: IQueryHandler<GetYearInReviewQuery, YearInReviewResponse> {

	public async Task<YearInReviewResponse> Handle(
		GetYearInReviewQuery request,
		CancellationToken cancellationToken) {
		if (request.Year is < 1 or > 9998) {
			throw new ArgumentOutOfRangeException(nameof(request.Year));
		}

		var start = new DateTime(request.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		var end = start.AddYears(1);
		var events = context.PerfumeEvents
			.AsNoTracking()
			.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn
				&& x.EventDate >= start
				&& x.EventDate < end);

		var totalWears = await events.CountAsync(cancellationToken);
		if (totalWears == 0) {
			return new YearInReviewResponse(
				request.Year, 0, 0, 0, 0, [], [], [], null, null, []);
		}

		var uniquePerfumes = await events
			.Select(x => x.PerfumeId)
			.Distinct()
			.CountAsync(cancellationToken);
		var uniqueHouses = await events
			.Select(x => x.Perfume.House)
			.Distinct()
			.CountAsync(cancellationToken);
		var activeDays = await events
			.Select(x => x.EventDate.Date)
			.Distinct()
			.CountAsync(cancellationToken);

		var perfumeRows = await events
			.GroupBy(x => new {
				x.PerfumeId,
				x.Perfume.PerfumeName,
				x.Perfume.House,
				x.Perfume.Family,
				x.Perfume.ImageObjectKeyNew
			})
			.Select(group => new {
				group.Key.PerfumeId,
				group.Key.PerfumeName,
				group.Key.House,
				group.Key.Family,
				group.Key.ImageObjectKeyNew,
				Count = group.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.PerfumeName)
			.ToListAsync(cancellationToken);

		var topPerfumes = perfumeRows.Take(5).Select(x => {
			var imageUrl = presignedUrlService.GetUrl(
				x.ImageObjectKeyNew,
				Amazon.S3.HttpVerb.GET)?.ToString();
			return new YearInReviewRankedItem(
				x.PerfumeName, x.House, x.Count, ImageUrl: imageUrl);
		}).ToList();

		var topHouseRows = await events
			.GroupBy(x => x.Perfume.House)
			.Select(group => new { Name = group.Key, Count = group.Count() })
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Name)
			.Take(5)
			.ToListAsync(cancellationToken);
		var topHouses = topHouseRows
			.Select(x => new YearInReviewRankedItem(x.Name, null, x.Count))
			.ToList();

		var topTagRows = await events
			.SelectMany(x => x.Perfume.PerfumeTags)
			.GroupBy(x => new { x.Tag.Id, x.Tag.TagName, x.Tag.Color })
			.Select(group => new {
				Name = group.Key.TagName,
				group.Key.Color,
				Count = group.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.Name)
			.Take(5)
			.ToListAsync(cancellationToken);
		var topTags = topTagRows
			.Select(x => new YearInReviewRankedItem(
				x.Name, null, x.Count, x.Color))
			.ToList();

		var wearRows = await events
			.Select(x => new { x.PerfumeId, x.EventDate })
			.ToListAsync(cancellationToken);
		var busiestMonth = wearRows
			.Select(x => x.EventDate)
			.GroupBy(x => x.Month)
			.OrderByDescending(x => x.Count())
			.ThenBy(x => x.Key)
			.Select(x => new YearInReviewRankedItem(
				System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Key),
				null,
				x.Count()))
			.First();
		var busiestDay = wearRows
			.Select(x => x.EventDate)
			.GroupBy(x => x.DayOfWeek)
			.OrderByDescending(x => x.Count())
			.ThenBy(x => x.Key)
			.Select(x => new YearInReviewRankedItem(x.Key.ToString(), null, x.Count()))
			.First();

		var perfumeIds = perfumeRows.Select(x => x.PerfumeId).ToList();
		var ratingRows = await context.PerfumeRatings
			.AsNoTracking()
			.Where(x => perfumeIds.Contains(x.PerfumeId) && x.RatingDate < end)
			.Select(x => new { x.PerfumeId, x.RatingDate, x.Rating })
			.OrderBy(x => x.RatingDate)
			.ToListAsync(cancellationToken);
		var tagRows = await context.PerfumeTags
			.AsNoTracking()
			.Where(x => perfumeIds.Contains(x.PerfumeId))
			.Select(x => new {
				x.PerfumeId,
				TagId = x.Tag.Id,
				Name = x.Tag.TagName,
				x.Tag.Description
			})
			.ToListAsync(cancellationToken);

		var latestRatings = ratingRows
			.GroupBy(x => x.PerfumeId)
			.ToDictionary(x => x.Key, x => (decimal?)x.Last().Rating);
		var tagsByPerfume = tagRows
			.GroupBy(x => x.PerfumeId)
			.ToDictionary(
				x => x.Key,
				x => (IReadOnlyList<YearInReviewAiTag>)x.Select(t =>
					new YearInReviewAiTag(t.TagId, t.Name, t.Description)).ToList());
		var rowById = perfumeRows.ToDictionary(x => x.PerfumeId);

		var sampleReasons = new Dictionary<Guid, List<string>>();
		void AddSample(IEnumerable<Guid> ids, string reason) {
			foreach (var id in ids) {
				if (!sampleReasons.TryGetValue(id, out var reasons)) {
					reasons = [];
					sampleReasons.Add(id, reasons);
				}
				if (!reasons.Contains(reason, StringComparer.Ordinal)) reasons.Add(reason);
			}
		}

		AddSample(perfumeRows.Take(5).Select(x => x.PerfumeId), "most-worn");
		AddSample(perfumeRows
			.Where(x => latestRatings.ContainsKey(x.PerfumeId))
			.OrderByDescending(x => latestRatings[x.PerfumeId])
			.ThenByDescending(x => x.Count)
			.Take(5)
			.Select(x => x.PerfumeId), "top-rated");
		AddSample(perfumeRows
			.Where(x => latestRatings.GetValueOrDefault(x.PerfumeId) >= 8m)
			.OrderBy(x => x.Count)
			.ThenByDescending(x => latestRatings[x.PerfumeId])
			.Take(5)
			.Select(x => x.PerfumeId), "high-rated-low-use");

		var tagFrequency = tagRows
			.GroupBy(x => x.TagId)
			.ToDictionary(x => x.Key, x => x.Count());

		var candidates = sampleReasons.Take(20).Select(sample => {
			var id = sample.Key;
			var row = rowById[id];
			return new YearInReviewAiCandidate(
				id,
				row.House,
				row.PerfumeName,
				row.Family,
				row.Count,
				latestRatings.GetValueOrDefault(id),
				tagsByPerfume.GetValueOrDefault(id, [])
					.OrderBy(t => tagFrequency[t.TagId])
					.Take(10)
					.Select(t => t with {
						Description = t.Description is { Length: > 160 }
							? t.Description[..160]
							: t.Description
					})
					.ToList(),
				sample.Value.AsReadOnly());
		}).ToList();

		var categories = new List<YearInReviewCategory>();
		string? GetImageUrl(Guid perfumeId) => presignedUrlService.GetUrl(
			rowById[perfumeId].ImageObjectKeyNew,
			Amazon.S3.HttpVerb.GET)?.ToString();

		var hiddenGem = ratingRows
			.GroupBy(x => x.PerfumeId)
			.Select(group => {
				var inYear = group.Where(x => x.RatingDate >= start).ToList();
				if (inYear.Count == 0) return null;
				var from = group.LastOrDefault(x => x.RatingDate < start)?.Rating
					?? inYear.First().Rating;
				var to = inYear.Last().Rating;
				return new { PerfumeId = group.Key, From = from, To = to, Change = to - from };
			})
			.Where(x => x != null && x.Change > 0)
			.OrderByDescending(x => x!.Change)
			.FirstOrDefault();
		if (hiddenGem != null) {
			var row = rowById[hiddenGem.PerfumeId];
			categories.Add(new YearInReviewCategory(
				"hiddenGem",
				"Your Hidden Gem",
				row.PerfumeName,
				row.House,
				GetImageUrl(row.PerfumeId),
				$"Your rating climbed by {hiddenGem.Change:0.#} points this year.",
				hiddenGem.From,
				hiddenGem.To));
		}

		var forgottenFavourite = wearRows
			.GroupBy(x => x.PerfumeId)
			.Where(x => latestRatings.GetValueOrDefault(x.Key) >= 8m && x.Count() >= 2)
			.Select(group => {
				var dates = group.Select(x => x.EventDate).OrderBy(x => x).ToList();
				var gap = dates.Zip(dates.Skip(1), (left, right) => (right - left).Days).Max();
				return new { PerfumeId = group.Key, GapDays = gap };
			})
			.OrderByDescending(x => x.GapDays)
			.FirstOrDefault();
		if (forgottenFavourite != null) {
			var row = rowById[forgottenFavourite.PerfumeId];
			categories.Add(new YearInReviewCategory(
				"forgottenFavourite",
				"Your Forgotten Favourite",
				row.PerfumeName,
				row.House,
				GetImageUrl(row.PerfumeId),
				$"A {forgottenFavourite.GapDays}-day gap, but still rated {latestRatings[row.PerfumeId]:0.#}.",
				GapDays: forgottenFavourite.GapDays));
		}

		var aiSelection = await (yearInReviewAi ?? new NullYearInReviewAi())
			.SelectCategories(request.Year, candidates, cancellationToken);
		if (aiSelection != null) {
			void AddAiPerfumeCategory(string key, string title, Guid? perfumeId, string? reason) {
				var candidate = candidates.FirstOrDefault(x => x.PerfumeId == perfumeId);
				if (candidate == null || string.IsNullOrWhiteSpace(reason)) return;
				categories.Add(new YearInReviewCategory(
					key, title, candidate.PerfumeName, candidate.House,
					GetImageUrl(candidate.PerfumeId), reason.Trim()));
			}

			AddAiPerfumeCategory("crowdPleaser", "Your Crowd Pleaser",
				aiSelection.CrowdPleaserPerfumeId, aiSelection.CrowdPleaserReason);
			AddAiPerfumeCategory("wildcard", "Your Wildcard",
				aiSelection.WildcardPerfumeId, aiSelection.WildcardReason);
			AddAiPerfumeCategory("comfortScent", "Your Comfort Scent",
				aiSelection.ComfortScentPerfumeId, aiSelection.ComfortScentReason);
		}

		var categoryOrder = new[] {
			"crowdPleaser", "wildcard", "hiddenGem", "comfortScent", "forgottenFavourite"
		};
		categories = categories
			.OrderBy(x => Array.IndexOf(categoryOrder, x.Key))
			.ToList();

		return new YearInReviewResponse(
			request.Year,
			totalWears,
			uniquePerfumes,
			uniqueHouses,
			activeDays,
			topPerfumes,
			topHouses,
			topTags,
			busiestMonth,
			busiestDay,
			categories);
	}

}
