using Pgvector;

namespace PerfumeTracker.Server.Features.Embedding;

public class NullEncoder : IEncoder {
	public Task<Vector> GetEmbeddings(string text, CancellationToken cancellationToken) {
		return Task.FromResult(new Vector(new float[1536]));
	}
}
