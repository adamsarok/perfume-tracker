using PerfumeTracker.Server.Features.Common;
using System.Text;

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
				presignedUrlService?.GetUrl(p.ImageObjectKeyNew, Amazon.S3.HttpVerb.GET)?.ToString() ?? "",
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

	public static PerfumeLlmDto ToPerfumeLlmDto(this Perfume p) {
		var lastComment = p.PerfumeRatings.Any()
			? p.PerfumeRatings.OrderByDescending(x => x.RatingDate).First().Comment
			: null;

		return new PerfumeLlmDto(
			Id: p.Id,
			House: p.House,
			PerfumeName: p.PerfumeName,
			Family: p.Family,
			Rating: p.AverageRating,
			TimesWorn: p.WearCount,
			Tags: [.. p.PerfumeTags.Select(pt => pt.Tag.TagName)],
			LastComment: lastComment
		);
	}

	public static PerfumeLlmDto ToPerfumeLlmDto(this PerfumeWithWornStatsDto p) {
		return new PerfumeLlmDto(
			Id: p.Perfume.Id,
			House: p.Perfume.House,
			PerfumeName: p.Perfume.PerfumeName,
			Family: p.Perfume.Family,
			Rating: p.averageRating,
			TimesWorn: p.WornTimes,
			Tags: [.. p.Perfume.Tags.Select(t => t.TagName)],
			LastComment: p.lastComment
		);
	}

	public static string GetTextForEmbedding(this Perfume perfume) {
		var sb = new StringBuilder(); // only add fields which are not available in perfume. Eg House can be searched in Perfume - sentiment based on all tags & comments can not
		if (!string.IsNullOrWhiteSpace(perfume.Family)) {
			sb.Append($"Family: {perfume.Family}. ");
		}

		if (perfume.PerfumeTags.Any()) {
			sb.Append($"Notes: {string.Join(", ", perfume.PerfumeTags.Select(pt => pt.Tag.TagName))}. ");
		}

		if (perfume.PerfumeRatings.Any()) {
			sb.Append($"Rating: {perfume.PerfumeRatings.Average(x => x.Rating).ToString("0.#")}/10 points. ");
			sb.Append($"User comments: ");
			var comments = perfume.PerfumeRatings
				.Where(r => !string.IsNullOrWhiteSpace(r.Comment))
				.OrderByDescending(r => r.UpdatedAt)
				.Take(10)
				.Select(r => r.Comment);
			if (comments.Any()) {
				sb.Append($"{string.Join(". ", comments)}. ");
			}
		}

		var wearEvents = perfume.PerfumeEvents.Where(x => x.Type == PerfumeEvent.PerfumeEventType.Worn);
		if (wearEvents.Any()) {
			sb.Append($"Worn {wearEvents.Count()} times, last on {wearEvents.Max(x => x.EventDate).ToString("yyyy-MM-dd")}.");
		}

		return sb.ToString();
	}
}
