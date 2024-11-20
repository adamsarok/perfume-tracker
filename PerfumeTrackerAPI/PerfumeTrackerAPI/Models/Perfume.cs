using System;
using System.Collections.Generic;

namespace PerfumeTrackerAPI.Models;

public partial class Perfume
{
    public int Id { get; set; }

    public string House { get; set; } = null!;

    public string Perfume1 { get; set; } = null!;

    public double Rating { get; set; }

    public string Notes { get; set; } = null!;

    public int Ml { get; set; }

    public string ImageObjectKey { get; set; } = null!;

    public bool Autumn { get; set; }

    public bool Spring { get; set; }

    public bool Summer { get; set; }

    public bool Winter { get; set; }

    public virtual ICollection<PerfumeSuggested> PerfumeSuggesteds { get; set; } = new List<PerfumeSuggested>();

    public virtual ICollection<PerfumeTag> PerfumeTags { get; set; } = new List<PerfumeTag>();

    public virtual ICollection<PerfumeWorn> PerfumeWorns { get; set; } = new List<PerfumeWorn>();
}
