using Microsoft.Extensions.Options;
using OpenAI;
using Pgvector;

namespace PerfumeTracker.Server.Services.Embedding;

public class Encoder(OpenAIClient client, IOptions<OpenAIOptions> options) : IEncoder {
	public async Task<Vector> GetEmbeddings(string text, CancellationToken cancellationToken) {
		var embeddingClient = client.GetEmbeddingClient(options.Value.EmbeddingsModel);
		var response = await embeddingClient.GenerateEmbeddingAsync(text, null, cancellationToken);
		var embedding = response.Value.ToFloats();
		return new Vector(embedding.ToArray());
	}
}
