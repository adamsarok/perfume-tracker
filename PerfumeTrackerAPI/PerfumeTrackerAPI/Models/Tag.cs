using System;
using System.Collections.Generic;

namespace PerfumeTrackerAPI.Models;

public partial class Tag
{
    public string TagName { get; set; } = null!;

    public string Color { get; set; } = null!;

    public int Id { get; set; }
    public DateTime Created_At { get; set; }
    public DateTime? Updated_At { get; set; }
    public virtual ICollection<PerfumeTag> PerfumeTags { get; set; } = new List<PerfumeTag>();
}
