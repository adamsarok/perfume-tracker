namespace PerfumeTracker.Server.Models;
public partial class PerfumeRandoms : Entity
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public virtual Perfume Perfume { get; set; } = null!;
    public bool IsAccepted { get; set; }
}
