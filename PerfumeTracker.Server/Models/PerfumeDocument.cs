using Pgvector;

namespace PerfumeTracker.Server.Models;

/// <summary>
/// Document model containing all perfume related data for embedding/search
/// </summary>
public class PerfumeDocument : UserEntity {
	public string Text { get; set; } = string.Empty;

	[Column(TypeName = "vector(1536)")]
	public Vector? Embedding { get; set; }

	public virtual Perfume Perfume { get; set; } = null!;
}
