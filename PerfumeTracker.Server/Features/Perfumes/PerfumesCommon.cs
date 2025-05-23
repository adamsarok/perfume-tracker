namespace PerfumeTracker.Server.Features.Perfumes;

public static class PerfumesCommon {
	public static PerfumeWithWornStatsDto ToPerfumeWithWornStatsDto(Perfume p, UserProfile userProfile) {
		decimal burnRatePerYearMl = 0;
		decimal yearsLeft = 0;
		p.MlLeft = Math.Max(0, p.PerfumeEvents.Sum(e => e.AmountMl));
		var worns = p.PerfumeEvents.Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn).ToList();
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
		return new PerfumeWithWornStatsDto(
				 new PerfumeDto(
					p.Id,
					p.House,
					p.PerfumeName,
					p.Rating,
					p.Notes,
					p.Ml,
					p.MlLeft,
					p.ImageObjectKey,
					p.Autumn,
					p.Spring,
					p.Summer,
					p.Winter,
					p.PerfumeTags.Select(tag => new TagDto(tag.Tag.TagName, tag.Tag.Color, tag.Tag.Id)).ToList()
				  ),
				  worns.Any() ? worns.Count : 0,
				  worns.Any() ? worns.Max(x => x.CreatedAt) : null,
				  burnRatePerYearMl,
				  yearsLeft
				  );
	}
}
