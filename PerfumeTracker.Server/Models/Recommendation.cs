using System;
using System.Collections.Generic;

namespace PerfumeTrackerAPI.Models;

public partial class Recommendation
{
    public int Id { get; set; }

    public string Query { get; set; } = null!;

    public string Recommendations { get; set; } = null!;

    public DateTime Created_At { get; set; }
}
