using Pgvector;

namespace PerfumeTracker.Server.Services.Embedding;

public class NullEncoder : IEncoder {
	public Task<Vector> GetEmbeddings(string text) {
		return Task.FromResult(new Vector(Array.Empty<float>()));
	}
}
