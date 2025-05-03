namespace PerfumeTracker.Server.Models;

public class Settings : Entity {
    public string UserId { get; set; } = null!;
    public double MinimumRating { get; set; }
    public int DayFilter { get; set; }
    public bool ShowMalePerfumes { get; set; }
    public bool ShowUnisexPerfumes { get; set; }
    public bool ShowFemalePerfumes { get; set; }
    public decimal SprayAmountFullSizeMl { get; set; }
	public decimal SprayAmountSamplesMl { get; set; }
	public decimal SprayAmountForBottleSize(decimal bottleSizeMl) => bottleSizeMl >= 20 ? SprayAmountFullSizeMl : SprayAmountSamplesMl;
}
