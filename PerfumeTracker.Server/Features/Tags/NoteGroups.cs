namespace PerfumeTracker.Server.Features.Tags;

public static class NoteGroups {
	public const string Other = "Other";

	public static readonly IReadOnlyList<string> All = [
		"Citrus",
		"Fruity",
		"Green",
		"Aromatic",
		"Herbal",
		"Floral",
		"White Floral",
		"Rose",
		"Powdery",
		"Spicy",
		"Sweet",
		"Gourmand",
		"Vanilla",
		"Amber",
		"Woody",
		"Resinous",
		"Leather",
		"Animalic",
		"Musk",
		"Aquatic",
		"Ozonic",
		"Earthy",
		"Tobacco",
		"Boozy",
		"Tea",
		"Synthetic",
		Other
	];

	public static readonly IReadOnlyList<string> WardrobeCoverage = All
		.Where(noteGroup => noteGroup != Other)
		.ToList();

	public static string ToPromptList() => string.Join(", ", All);
}
