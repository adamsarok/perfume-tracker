using System;
using System.Collections.Generic;

namespace PerfumeTracker.Server.Models;

public partial class PerfumeWorn
{
    public int Id { get; set; }

    public int PerfumeId { get; set; }

    public DateTime Created_At { get; set; }

    public virtual Perfume Perfume { get; set; } = null!;
}
