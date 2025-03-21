namespace PerfumeTracker.Server.Models {
	public class PerfumePlayList {
		public string Name { get; set; }
		public virtual ICollection<Perfume> Perfumes { get; set; } = new List<Perfume>();
		public DateTime Created_At { get; set; }
		public DateTime? Updated_At { get; set; }
	}
}
