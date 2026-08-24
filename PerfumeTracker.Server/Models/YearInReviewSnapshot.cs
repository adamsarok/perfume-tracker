namespace PerfumeTracker.Server.Models;

public class YearInReviewSnapshot : UserEntity {
	public int Year { get; set; }
	public string Payload { get; set; } = null!;
}
