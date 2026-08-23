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
	YearInReviewRankedItem? BusiestDay
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
	IPresignedUrlService presignedUrlService)
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
				request.Year, 0, 0, 0, 0, [], [], [], null, null);
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

		var topPerfumeRows = await events
			.GroupBy(x => new {
				x.PerfumeId,
				x.Perfume.PerfumeName,
				x.Perfume.House,
				x.Perfume.ImageObjectKeyNew
			})
			.Select(group => new {
				group.Key.PerfumeId,
				group.Key.PerfumeName,
				group.Key.House,
				group.Key.ImageObjectKeyNew,
				Count = group.Count()
			})
			.OrderByDescending(x => x.Count)
			.ThenBy(x => x.PerfumeName)
			.Take(5)
			.ToListAsync(cancellationToken);

		var topPerfumes = topPerfumeRows.Select(x => {
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

		var dateCounts = await events
			.Select(x => x.EventDate)
			.ToListAsync(cancellationToken);
		var busiestMonth = dateCounts
			.GroupBy(x => x.Month)
			.OrderByDescending(x => x.Count())
			.ThenBy(x => x.Key)
			.Select(x => new YearInReviewRankedItem(
				System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(x.Key),
				null,
				x.Count()))
			.First();
		var busiestDay = dateCounts
			.GroupBy(x => x.DayOfWeek)
			.OrderByDescending(x => x.Count())
			.ThenBy(x => x.Key)
			.Select(x => new YearInReviewRankedItem(x.Key.ToString(), null, x.Count()))
			.First();

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
			busiestDay);
	}
}
