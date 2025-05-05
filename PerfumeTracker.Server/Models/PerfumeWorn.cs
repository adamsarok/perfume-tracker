namespace PerfumeTracker.Server.Models;

public partial class PerfumeWorn : Entity
{
    public int Id { get; set; }
    public int PerfumeId { get; set; }
    public virtual Perfume Perfume { get; set; } = null!;
	public DateTime WornOn { get; set; } = DateTime.UtcNow;
}
