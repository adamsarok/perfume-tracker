using System.Security.Cryptography;
using System.Text;

namespace PerfumeTracker.Evals.Harness;

/// <summary>
/// The fixed wardrobe every eval case runs against. Ids are derived from the perfume key so
/// recorded model responses (which contain owned perfume links) stay valid across runs.
/// Ratings and wear counts are deliberately distinct so the generated system prompt is stable.
/// </summary>
public static class EvalWardrobe {
	private const string IdNamespace = "perfume-tracker-evals";

	public sealed record SeedPerfume(
		string Key,
		string House,
		string PerfumeName,
		string Family,
		string Parfumeur,
		decimal Rating,
		int WearCount,
		int? DaysSinceLastWorn,
		decimal Ml,
		decimal MlLeft,
		string[] Tags,
		string? Comment) {
		public string DisplayName => $"{House} - {PerfumeName}";
		public Guid Id => EvalWardrobe.Id("perfume", Key);
	}

	public sealed record SeedOffer(
		string Source,
		string SourceOfferId,
		string? SellerName,
		string? BrandName,
		string ProductName,
		string OfferTitle,
		string? PriceText,
		string? SizeText,
		string Status,
		int DaysSinceListed);

	public static Guid Id(string kind, string name) {
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{IdNamespace}:{kind}:{name}"));
		var guidBytes = bytes[..16];
		// RFC 4122 version 5 / variant bits so the ids look like normal Guids.
		guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x50);
		guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);
		return new Guid(guidBytes);
	}

	public static readonly IReadOnlyDictionary<string, string?> TagNoteGroups = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase) {
		["iris"] = "Floral",
		["musk"] = "Musk",
		["powdery"] = "Powdery",
		["cedar"] = "Woody",
		["vetiver"] = "Earthy",
		["bergamot"] = "Citrus",
		["amber"] = "Amber",
		["vanilla"] = "Vanilla",
		["peony"] = "Floral",
		["rose"] = "Rose",
		["lemon"] = "Citrus",
		["neroli"] = "White Floral",
		["gardenia"] = "White Floral",
		["jasmine"] = "Floral",
		["sandalwood"] = "Woody",
		["incense"] = "Resinous",
		["white tea"] = "Tea",
		["tobacco"] = "Tobacco",
		["cacao"] = "Gourmand",
		["patchouli"] = "Earthy",
		["pine"] = "Woody",
		["clove"] = "Spicy",
		["cashmeran"] = "Synthetic",
		["geranium"] = "Floral",
		// Deliberately ungrouped so the wardrobe-gap analysis reports ungrouped perfumes.
		["musk oil"] = null
	};

	public static IReadOnlyList<SeedPerfume> Perfumes { get; } = [
		new("velvet-iris", "Maison Lumiere", "Velvet Iris", "Floral", "A. Moreau", 4.75m, 24, 12, 100m, 62m,
			["iris", "musk", "powdery"], "Signature iris, always gets a compliment."),
		new("cedar-storm", "Atelier Nord", "Cedar Storm", "Woody", "B. Lindqvist", 4.5m, 18, 30, 100m, 41m,
			["cedar", "vetiver", "bergamot"], "Sharp cedar opening that fades nicely."),
		new("amber-resin", "Casa Aurelia", "Amber Resin", "Oriental", "C. Delgado", 4.4m, 15, 55, 50m, 20m,
			["amber", "vanilla"], "Best on cold evenings."),
		new("iris-cashmere", "Fleur & Co", "Iris Cashmere", "Floral", "D. Fontaine", 4.3m, 14, 25, 50m, 44m,
			["iris", "cashmeran"], null),
		new("peony-bloom", "Fleur & Co", "Peony Bloom", "Floral", "D. Fontaine", 4.25m, 12, 8, 75m, 55m,
			["peony", "rose"], null),
		new("bergamot-sky", "Citrus Grove", "Bergamot Sky", "Citrus", "E. Rossi", 4.1m, 11, 3, 100m, 70m,
			["bergamot", "lemon", "neroli"], "Great office scent."),
		new("gardenia-veil", "Maison Lumiere", "Gardenia Veil", "Floral", "A. Moreau", 4.0m, 9, 40, 50m, 33m,
			["gardenia", "jasmine"], null),
		new("sandalwood-ritual", "Nordwind", "Sandalwood Ritual", "Woody", "B. Lindqvist", 3.9m, 7, 90, 100m, 12m,
			["sandalwood", "incense"], "Feels meditative."),
		new("lemon-silk", "Bella Vita", "Lemon Silk", "Citrus", "F. Marchetti", 3.75m, 6, 20, 30m, 25m,
			["lemon", "white tea", "musk"], null),
		new("vanilla-smoke", "Casa Aurelia", "Vanilla Smoke", "Gourmand", "C. Delgado", 3.6m, 5, 120, 50m, 8m,
			["vanilla", "tobacco", "cacao"], "A bit too sweet in summer."),
		new("rose-absolue", "Fleur & Co", "Rose Absolue", "Floral", "D. Fontaine", 3.4m, 4, 200, 30m, 18m,
			["rose", "geranium", "patchouli"], null),
		new("winter-wood", "Citrus Grove", "Winter Wood", "Woody", "E. Rossi", 3.0m, 2, 300, 100m, 60m,
			["pine", "cedar", "clove"], null),
		new("trial-bloom", "Maison Lumiere", "Trial Bloom", "Floral", "A. Moreau", 0m, 0, null, 10m, 10m,
			["iris"], "Sample from a swap, still need to test it properly."),
		new("empty-vetiver", "Atelier Nord", "Empty Vetiver", "Woody", "B. Lindqvist", 3.5m, 10, 400, 100m, 0m,
			["vetiver"], "Bottle is finished."),
		new("mystery-musk", "Bella Vita", "Mystery Musk", "Musk", "F. Marchetti", 0m, 0, null, 10m, 10m,
			["musk oil"], null),
		new("rebuy-amber", "Rebuy Lab", "Last Amber", "Oriental", "E. Example", 4.3m, 8, 5, 100m, 9m,
			["amber"], "A favorite running low."),
		new("rebuy-boundary", "Rebuy Lab", "Ten Percent", "Woody", "E. Example", 4.2m, 8, 10, 50m, 5m,
			["cedar"], "Exactly ten percent remains."),
		new("rebuy-sample", "Rebuy Lab", "Full Sample", "Floral", "E. Example", 4.1m, 1, 2, 2m, 1.9m,
			["rose"], "Small but almost full."),
		new("rebuy-rating-boundary", "Rebuy Lab", "Average Rose", "Floral", "E. Example", 3m, 8, 6, 100m, 2m,
			["rose"], "Not a favorite."),
		new("rebuy-unrated", "Rebuy Lab", "Unrated Amber", "Oriental", "E. Example", 0m, 1, 7, 100m, 1m,
			["amber"], "Not rated yet."),
		new("rebuy-unknown", "Rebuy Lab", "Unknown Capacity", "Woody", "E. Example", 4m, 8, 10, 0m, 1m,
			["cedar"], "Capacity is unknown.")
	];

	public static IReadOnlyList<SeedOffer> Offers { get; } = [
		new("Bazos", "bz-1001", "pete_deals", "Maison Lumiere", "Velvet Iris", "Maison Lumiere Velvet Iris 50ml decant",
			"45 EUR", "50ml", "Active", 2),
		new("Bazos", "bz-1002", "pete_deals", "Atelier Nord", "Pine Ritual", "Atelier Nord Pine Ritual 100ml",
			"80 EUR", "100ml", "Active", 5),
		new("AromaSwap", "as-77", "anna_k", "Casa Aurelia", "Amber Resin", "Casa Aurelia Amber Resin 10ml decant",
			"12 EUR", "10ml", "Active", 1),
		new("AromaSwap", "as-78", "rosa_m", "Tauer", "Incense Extreme", "Tauer Incense Extreme 50ml partial",
			"60 EUR", "50ml", "Active", 3),
		new("Bazos", "bz-1003", "pete_deals", "Nordwind", "Sandalwood Ritual", "Nordwind Sandalwood Ritual 5ml sample",
			"9 EUR", "5ml", "Sold", 30),
		new("Bazos", "bz-1004", "pete_deals", "Bella Vita", "Lemon Silk", "Bella Vita Lemon Silk 30ml",
			"20 EUR", "30ml", "Active", 7)
	];

	public static IReadOnlyList<string> TagNames { get; } = TagNoteGroups.Keys.OrderBy(name => name, StringComparer.Ordinal).ToList();

	public static string DisplayNameForPerfumeId(Guid perfumeId) =>
		Perfumes.FirstOrDefault(p => p.Id == perfumeId)?.DisplayName ?? perfumeId.ToString();
}
