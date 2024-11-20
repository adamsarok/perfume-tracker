using System;
using System.Collections.Generic;

namespace PerfumeTrackerAPI.Models;

public partial class Tag
{
    public string Tag1 { get; set; } = null!;

    public string Color { get; set; } = null!;

    public int Id { get; set; }

    public virtual ICollection<PerfumeTag> PerfumeTags { get; set; } = new List<PerfumeTag>();
}
