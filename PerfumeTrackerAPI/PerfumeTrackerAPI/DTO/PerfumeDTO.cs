namespace PerfumeTrackerAPI.DTO {
    public class PerfumeDTO {
        //TODO: why are these still classes not records?
        public int Id { get; set; }

        public string House { get; set; } = null!;

        public string PerfumeName { get; set; } = null!;

        public double Rating { get; set; }

        public string Notes { get; set; } = null!;

        public int Ml { get; set; }

        public string ImageObjectKey { get; set; } = null!;

        public bool Autumn { get; set; }

        public bool Spring { get; set; }

        public bool Summer { get; set; }

        public bool Winter { get; set; }
        public List<TagDTO> Tags { get; set; } = new();
    }
}
