namespace PerfumeTracker.Server.Models;

public partial class Recommendation : Entity
{
    public int Id { get; set; }

    public string Query { get; set; } = null!;

    public string Recommendations { get; set; } = null!;
}
