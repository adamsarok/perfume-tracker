using Pgvector;

namespace PerfumeTracker.Server.Services.Embedding;

public class NullEncoder : IEncoder {
	public Task<Vector> GetEmbeddings(string text, CancellationToken cancellationToken) {
		return Task.FromResult(new Vector(Array.Empty<float>()));
	}
}
