namespace PerfumeTracker.Server.Models;

public partial class Perfume : UserEntity {
	public string House { get; set; } = null!;
	public string PerfumeName { get; set; } = null!;
	public string Family { get; set; } = null!;
	public decimal Ml { get; set; }
	public decimal MlLeft { get; set; }
	public decimal AverageRating { get; set; }
	public int WearCount { get; set; }
	public Guid? ImageObjectKeyNew { get; set; } = null!;
	public NpgsqlTsVector FullText { get; set; } = null!;
	public virtual ICollection<PerfumeRecommendation> PerfumeRecommendations { get; } = [];
	public virtual ICollection<PerfumeTag> PerfumeTags { get; } = [];
	public virtual ICollection<PerfumeEvent> PerfumeEvents { get; } = [];
	public virtual ICollection<PerfumeRating> PerfumeRatings { get; } = [];
	public virtual PerfumeDocument? PerfumeDocument { get; set; }
	public bool IsIdentifyBackfillFailed { get; set; }
}