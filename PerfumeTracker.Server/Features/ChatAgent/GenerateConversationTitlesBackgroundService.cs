using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.Server.Features.ChatAgent;

public class GenerateConversationTitlesBackgroundService(
	IServiceProvider serviceProvider,
	ILogger<GenerateConversationTitlesBackgroundService> logger) : BackgroundService {
	protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
		await Task.Yield();

		using var scope = serviceProvider.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		var chatAgent = scope.ServiceProvider.GetRequiredService<IChatAgent>();
		var conversationIds = await context.Set<ChatConversation>()
			.AsNoTracking()
			.IgnoreQueryFilters()
			.Where(conversation => conversation.TitleGeneratedAt == null)
			.Where(c => !c.IsDeleted)
			.OrderBy(conversation => conversation.CreatedAt)
			.Select(conversation => conversation.Id)
			.ToListAsync(stoppingToken);

		if (conversationIds.Count == 0) return;

		logger.LogInformation("Generating titles for {ConversationCount} existing chat conversations", conversationIds.Count);

		foreach (var conversationId in conversationIds) {
			if (stoppingToken.IsCancellationRequested) break;

			var conversation = await context.Set<ChatConversation>()
				.IgnoreQueryFilters()
				.FirstOrDefaultAsync(item => item.Id == conversationId, stoppingToken);
			if (conversation == null) continue;

			await chatAgent.GenerateAndSaveConversationTitle(conversation, stoppingToken);
		}

		logger.LogInformation("Finished generating titles for existing chat conversations");
	}
}
