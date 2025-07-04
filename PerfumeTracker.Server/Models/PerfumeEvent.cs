﻿namespace PerfumeTracker.Server.Models;

public partial class PerfumeEvent : UserEntity {
	public Guid PerfumeId { get; set; }
	public virtual Perfume Perfume { get; set; } = null!;
	public DateTime EventDate { get; set; } = DateTime.UtcNow;
	public enum PerfumeEventType {
		Added = 0,
		Worn = 1,
		Adjusted = 2
	}
	public PerfumeEventType Type { get; set; } = PerfumeEventType.Added;
	public decimal AmountMl { get; set; } = 0;
	public int SequenceNumber { get; set; }
}
