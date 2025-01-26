using System;
using System.Collections.Generic;

namespace PerfumeTrackerAPI.Models;

public partial class PerfumeSuggested
{
    public int Id { get; set; }

    public int PerfumeId { get; set; }

    public DateTime Created_At { get; set; }

    public virtual Perfume Perfume { get; set; } = null!;
}
