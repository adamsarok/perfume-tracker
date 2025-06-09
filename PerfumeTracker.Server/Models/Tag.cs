namespace PerfumeTracker.Server.Models;

public partial class Tag : UserEntity {
    public string TagName { get; set; } = null!;
    public string Color { get; set; } = null!;
    public virtual ICollection<PerfumeTag> PerfumeTags { get; set; } = new List<PerfumeTag>();
}
