using OpenAI.Chat;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Ai;

public record GetAiRecommendationQuery(string Query) : IQuery<string>;
public class GetAiRecommendationEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/ai", async (GetAiRecommendationQuery query, ISender sender, CancellationToken cancellationToken) => {
			var result = await sender.Send(query, cancellationToken);
			return Results.Ok(result);
		})
			.WithTags("Ai")
			.WithName("GetAiRecommendation")
			.RequireAuthorization(Policies.WRITE);
	}
}

public class GetAiRecommendationHandler(IConfiguration configuration) : IQueryHandler<GetAiRecommendationQuery, string> {
	public async Task<string> Handle(GetAiRecommendationQuery request, CancellationToken cancellationToken) {
		string? apiKey = configuration["OpenAi:ApiKey"];
		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("OpenAI API key is not configured");
		}
		ChatClient client = new(model: "gpt-4o-mini", apiKey: apiKey);
		List<ChatMessage> messages = [
			new SystemChatMessage("You are a perfume recommendation expert."),
					new UserChatMessage(request.Query)
		];
		ChatCompletion completion = await client.CompleteChatAsync(messages);
		return completion.Content[0].Text;
	}
}