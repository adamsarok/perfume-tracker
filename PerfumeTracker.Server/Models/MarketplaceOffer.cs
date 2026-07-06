namespace PerfumeTracker.Server.Models;

public class MarketplaceOffer : UserEntity {
	public string Source { get; set; } = null!;
	public string SourceOfferId { get; set; } = null!;
	public string? SourceUrl { get; set; }
	public string? SellerName { get; set; }
	public int? SellerExternalId { get; set; }
	public string? BrandName { get; set; }
	public string ProductName { get; set; } = null!;
	public string OfferTitle { get; set; } = null!;
	public string? PriceText { get; set; }
	public string? SizeText { get; set; }
	public string? Description { get; set; }
	public string Status { get; set; } = "Active";
	public DateTime? ListedAt { get; set; }
	public DateTime ImportedAt { get; set; } = DateTime.UtcNow;
	public Guid? MatchedPerfumeId { get; set; }
	public decimal? MatchConfidence { get; set; }
}
