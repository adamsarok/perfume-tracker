using System;
using System.Collections.Generic;

namespace PerfumeTrackerAPI.Models;

public partial class PerfumeTag
{
    public int Id { get; set; }

    public int PerfumeId { get; set; }

    public int TagId { get; set; }

    public virtual Perfume Perfume { get; set; } = null!;

    public virtual Tag Tag { get; set; } = null!;
}
