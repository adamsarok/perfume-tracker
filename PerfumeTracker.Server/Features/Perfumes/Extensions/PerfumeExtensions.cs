using PerfumeTracker.Server.Services.Common;

namespace PerfumeTracker.Server.Features.Perfumes.Extensions;

public static class PerfumeExtensions {
	public static PerfumeWithWornStatsDto ToPerfumeWithWornStatsDto(this Perfume p, UserProfile userProfile, IPresignedUrlService presignedUrlService) {
		decimal burnRatePerYearMl = 0;
		decimal yearsLeft = 0;
		var worns = p.PerfumeEvents.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn).ToList();
		if (p.MlLeft > 0 && worns.Any()) {
			var firstWorn = worns.Min(x => x.CreatedAt);
			var daysSinceFirstWorn = (DateTime.UtcNow - firstWorn).TotalDays;
			if (daysSinceFirstWorn >= 30 && worns.Count > 1) { //otherwise prediction will be inaccurate
				var spraysPerYear = 365 * (decimal)worns.Count / (decimal)(DateTime.UtcNow - firstWorn).TotalDays;
				var sprayAmountMl = userProfile.SprayAmountForBottleSize(p.Ml);
				if (sprayAmountMl > 0) {
					burnRatePerYearMl = spraysPerYear * userProfile.SprayAmountForBottleSize(p.Ml);
					yearsLeft = burnRatePerYearMl > 0 ? p.MlLeft / burnRatePerYearMl : 0;
				}
			}
		}
		string lastComment = "";
		if (p.PerfumeRatings.Any()) {
			lastComment = p.PerfumeRatings.OrderByDescending(x => x.RatingDate).First().Comment;
		}
		return new PerfumeWithWornStatsDto(
			new PerfumeDto(
				p.Id,
				p.House,
				p.PerfumeName,
				p.Family,
				p.Ml,
				p.MlLeft,
				p.ImageObjectKeyNew,
				presignedUrlService.GetUrl(p.ImageObjectKeyNew, Amazon.S3.HttpVerb.GET)?.ToString() ?? "",
				[.. p.PerfumeTags.Select(tag => new TagDto(tag.Tag.TagName, tag.Tag.Color, tag.Tag.Id, tag.Tag.IsDeleted))],
				p.IsDeleted,
				[.. p.PerfumeRatings.Select(r => new PerfumeRatings.PerfumeRatingDownloadDto(r.PerfumeId, r.Id, r.Rating, r.Comment, r.RatingDate, r.IsDeleted))]
			),
			p.WearCount,
			worns.Any() ? worns.Max(x => x.CreatedAt) : null,
			burnRatePerYearMl,
			yearsLeft,
			p.AverageRating,
			lastComment
		);
	}
}
