using System;
using System.Collections.Generic;

namespace PerfumeTrackerAPI.Models;

public partial class PerfumeWorn
{
    public int Id { get; set; }

    public int PerfumeId { get; set; }

    public DateTime WornOn { get; set; }

    public virtual Perfume Perfume { get; set; } = null!;
}
