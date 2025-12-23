using OpenAI;
using Pgvector;

namespace PerfumeTracker.Server.Services.Embedding;

public class Encoder(OpenAIClient client) : IEncoder {
	public async Task<Vector> GetEmbeddings(string text) {
		var embeddingClient = client.GetEmbeddingClient("text-embedding-3-small");
		var response = await embeddingClient.GenerateEmbeddingAsync(text);
		var embedding = response.Value.ToFloats();
		return new Vector(embedding.ToArray());
	}
}
