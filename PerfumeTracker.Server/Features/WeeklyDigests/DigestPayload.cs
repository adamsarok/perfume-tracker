using PerfumeTracker.Server.Features.Perfumes.Services;
using System.Text.Json.Serialization;

namespace PerfumeTracker.Server.Features.WeeklyDigests;

public static class DigestJson {
	public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web) {
		Converters = { new JsonStringEnumConverter() }
	};
}
public record DigestPick(Guid PerfumeId, string Reason, string? House = null, string? PerfumeName = null, bool IsAvailable = true);
public record DigestCategory(RecommendationStrategy Strategy, string Summary, string ShortageReason, List<DigestPick> Picks);
public record DigestPayload(string Introduction, string MoodOrOccasion, List<DigestCategory> Categories);

public static class DigestValidation {
	public static void Validate(DigestPayload payload, IReadOnlyDictionary<Guid, DigestCandidate> evidence, int totalEligible) {
		if (string.IsNullOrWhiteSpace(payload.Introduction) || string.IsNullOrWhiteSpace(payload.MoodOrOccasion)
			|| payload.Categories is null || payload.Categories.Count != 7
			|| payload.Categories.Select(c => c.Strategy).Distinct().Count() != 7)
			throw new InvalidDataException("Provide an introduction, weekly mood, and each of the seven categories exactly once.");
		foreach (var category in payload.Categories) {
			if (!Enum.IsDefined(category.Strategy) || string.IsNullOrWhiteSpace(category.Summary) || category.Picks is null
				|| category.Picks.Count > Math.Min(5, totalEligible) || category.Picks.Select(p => p.PerfumeId).Distinct().Count() != category.Picks.Count)
				throw new InvalidDataException("Each category needs a summary and up to five distinct eligible picks.");
			if (category.Picks.Count < Math.Min(5, totalEligible) && string.IsNullOrWhiteSpace(category.ShortageReason))
				throw new InvalidDataException("Explain why a category cannot supply five suitable perfumes.");
			if (totalEligible < 5 && string.IsNullOrWhiteSpace(category.ShortageReason))
				throw new InvalidDataException("Explain the small collection in each category.");
			foreach (var pick in category.Picks)
				if (!evidence.ContainsKey(pick.PerfumeId) || string.IsNullOrWhiteSpace(pick.Reason))
					throw new InvalidDataException("Every pick needs an evidenced perfume ID and explanation.");
		}
	}
}
