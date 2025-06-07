namespace PerfumeTracker.Server.Models;

public partial class Recommendation : UserEntity {
	public string Query { get; set; } = null!;
	public string Recommendations { get; set; } = null!;
}
