using OpenAI.Chat;

namespace PerfumeTracker.Server.Features.Ai;
public record GetAiRecommendationQuery(string Query) : IQuery<string>;
public class GetAiRecommendationEndpoint {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/ai", async (GetAiRecommendationQuery query, ISender sender) => {
			var result = await sender.Send(query);
			return Results.Ok(result);
		})
			.WithTags("Ai")
			.WithName("GetAiRecommendation");
	}
}

public class GetAiRecommendationHandler(IConfiguration configuration) : IQueryHandler<GetAiRecommendationQuery, string> {
	public async Task<string> Handle(GetAiRecommendationQuery request, CancellationToken cancellationToken) {
		//var configuration = context.RequestServices.GetService<IConfiguration>();
		string? apiKey = configuration["OpenAi:ApiKey"];
		ChatClient client = new(model: "gpt-4o-mini", apiKey: apiKey);
		List<ChatMessage> messages = [
			new SystemChatMessage("You are a perfume recommendation expert."),
					new UserChatMessage(request.Query)
		];
		ChatCompletion completion = client.CompleteChat(messages);
		return completion.Content[0].Text;
	}
}