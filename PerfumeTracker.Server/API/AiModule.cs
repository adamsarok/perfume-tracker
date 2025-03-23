using Carter;
using OpenAI.Chat;

namespace PerfumeTracker.Server.API {
	public class AiModule : ICarterModule {
		public record AiRequest(string Query);
		public void AddRoutes(IEndpointRouteBuilder app) {
			app.MapPost("/api/ai", async (AiRequest query, HttpContext context) => {
				var configuration = context.RequestServices.GetService<IConfiguration>();
				string? apiKey = configuration["OpenAi:ApiKey"];
				ChatClient client = new(model: "gpt-4o-mini", apiKey: apiKey);
				List<ChatMessage> messages =[
					new SystemChatMessage("You are a perfume recommendation expert."),
					new UserChatMessage(query.Query)
				];
				ChatCompletion completion = client.CompleteChat(messages);
				return Results.Ok(completion.Content[0].Text);
			})
				.WithTags("Ai")
				.WithName("GetAiRecommendation");
		}


	}
}
