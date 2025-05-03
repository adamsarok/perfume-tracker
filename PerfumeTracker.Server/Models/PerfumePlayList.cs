namespace PerfumeTracker.Server.Models {
	public class PerfumePlayList : Entity {
		public string Name { get; set; }
		public virtual ICollection<Perfume> Perfumes { get; set; } = new List<Perfume>();
	}
}
