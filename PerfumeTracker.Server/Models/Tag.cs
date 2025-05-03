using System;
using System.Collections.Generic;

namespace PerfumeTracker.Server.Models;

public partial class Tag : Entity
{
    public string TagName { get; set; } = null!;
    public string Color { get; set; } = null!;
    public int Id { get; set; }
    public virtual ICollection<PerfumeTag> PerfumeTags { get; set; } = new List<PerfumeTag>();
}
