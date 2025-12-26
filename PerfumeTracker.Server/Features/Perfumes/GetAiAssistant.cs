using Microsoft.Extensions.Options;
using OpenAI.Chat;
using PerfumeTracker.Server.Services.Auth;

namespace PerfumeTracker.Server.Features.Perfumes;

public class GetAiAssistantQueryValidator : AbstractValidator<GetAiAssistantQuery> {
	public GetAiAssistantQueryValidator() {
		RuleFor(x => x.Query).MinimumLength(10).MaximumLength(1000);
	}
}
public record GetAiAssistantQuery(string Query) : IQuery<string>;
public class GetAiAssistantEndpoint : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapPost("/api/ai-assistant", async (GetAiAssistantQuery query, ISender sender, CancellationToken cancellationToken) => {
			// This has to be revised completely
			// 1. Allow freetext?
			// 2. Test newer/bigger models
			var result = await sender.Send(query, cancellationToken);
			return Results.Ok(result);
		})
			.WithTags("Ai")
			.WithName("GetAiRecommendation")
			.RequireAuthorization(Policies.WRITE);
	}
}

public class GetAiAssistantQueryHandler(IOptions<OpenAIOptions> options) : IQueryHandler<GetAiAssistantQuery, string> {
	public async Task<string> Handle(GetAiAssistantQuery request, CancellationToken cancellationToken) {
		string? apiKey = options.Value.ApiKey;
		if (string.IsNullOrWhiteSpace(apiKey)) {
			throw new InvalidOperationException("OpenAI API key is not configured");
		}
		ChatClient client = new(model: options.Value.AssistantModel, apiKey: apiKey);
		List<ChatMessage> messages = [
			new SystemChatMessage("You are a perfume recommendation expert."),
					new UserChatMessage(request.Query)
		];
		ChatCompletion completion = await client.CompleteChatAsync(messages);
		return completion.Content[0].Text;
	}
}