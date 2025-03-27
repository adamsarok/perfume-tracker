using System;

namespace PerfumeTracker.Server.Models;

public class Settings {
    public string UserId { get; set; } = null!;
    public double MinimumRating { get; set; }
    public int DayFilter { get; set; }
    public bool ShowMalePerfumes { get; set; }
    public bool ShowUnisexPerfumes { get; set; }
    public bool ShowFemalePerfumes { get; set; }
    public decimal SprayAmount { get; set; }
    public DateTime Created_At { get; set; }
	public DateTime? Updated_At { get; set; }
}
