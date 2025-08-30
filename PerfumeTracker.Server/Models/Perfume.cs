namespace PerfumeTracker.Server.Models;

public partial class Perfume : UserEntity {
	public string House { get; set; } = null!;
	public string PerfumeName { get; set; } = null!;
	public decimal Ml { get; set; }
	[NotMapped]
	public decimal MlLeft { get; set; }
	public Guid? ImageObjectKeyNew { get; set; } = null!;
	public bool Autumn { get; set; }
	public bool Spring { get; set; }
	public bool Summer { get; set; }
	public bool Winter { get; set; }
	public NpgsqlTsVector FullText { get; set; } = null!;
	public virtual ICollection<PerfumeRandoms> PerfumeRandoms { get; } = [];
	public virtual ICollection<PerfumeTag> PerfumeTags { get; } = [];
	public virtual ICollection<PerfumeEvent> PerfumeEvents { get; } = [];
	public virtual ICollection<PerfumeRating> PerfumeRatings { get; } = [];
}
