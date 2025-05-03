namespace PerfumeTracker.Server.Models;

public partial class PerfumeSuggested : Entity
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public virtual Perfume Perfume { get; set; } = null!;
}
