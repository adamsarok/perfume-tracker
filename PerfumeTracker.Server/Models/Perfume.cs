namespace PerfumeTracker.Server.Models;

public partial class Perfume : Entity
{
    public int Id { get; set; }
    public string House { get; set; } = null!;
    public string PerfumeName { get; set; } = null!;
    public double Rating { get; set; }
    public string Notes { get; set; } = null!;
    public decimal Ml { get; set; }
	[NotMapped]
    public decimal MlLeft { get; set; }
    public string ImageObjectKey { get; set; } = null!;
    public bool Autumn { get; set; }
    public bool Spring { get; set; }
    public bool Summer { get; set; }
    public bool Winter { get; set; }
    public NpgsqlTsVector? FullText { get; set; } = null;
    public virtual ICollection<PerfumeSuggested> PerfumeSuggesteds { get; set; } = new List<PerfumeSuggested>();
    public virtual ICollection<PerfumeTag> PerfumeTags { get; set; } = new List<PerfumeTag>();
    public virtual ICollection<PerfumeWorn> PerfumeEvents { get; set; } = new List<PerfumeWorn>();
	public virtual ICollection<PerfumePlayList> PerfumePlayList { get; set; } = new List<PerfumePlayList>();
}
