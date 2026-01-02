using Pgvector;

namespace PerfumeTracker.Server.Features.Embedding;

public interface IEncoder {
	Task<Vector> GetEmbeddings(string text, CancellationToken cancellationToken);
}
