using PerfumeTracker.Server.Features.Perfumes.Services;

namespace PerfumeTracker.Server.Models;

public class PerfumeRecommendation : UserEntity {
	public Guid PerfumeId { get; set; }
	public virtual Perfume Perfume { get; set; } = null!;
	public RecommendationStrategy Strategy { get; set; }
	public Guid? CompletionId { get; set; }
	public virtual CachedCompletion? CachedCompletion { get; set; } = null!;
	public bool IsAccepted { get; set; }
}
