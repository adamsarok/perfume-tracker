namespace PerfumeTracker.Server.Models;

public partial class PerfumeRating : UserEntity {
	public Guid PerfumeId { get; set; }
	public virtual Perfume Perfume { get; set; } = null!;
	public DateTime RatingDate { get; set; } = DateTime.UtcNow;
	public decimal Rating { get; set; }
	public string Comment { get; set; } = string.Empty;
}
