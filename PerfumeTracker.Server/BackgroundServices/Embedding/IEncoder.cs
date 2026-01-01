using Pgvector;

namespace PerfumeTracker.Server.Services.Embedding;

public interface IEncoder {
	Task<Vector> GetEmbeddings(string text, CancellationToken cancellationToken);
}
