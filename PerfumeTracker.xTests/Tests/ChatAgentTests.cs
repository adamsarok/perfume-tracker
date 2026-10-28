#pragma warning disable OPENAI001 // Responses API is marked experimental in OpenAI.
using System.ClientModel.Primitives;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OpenAI.Chat;
using OpenAI.Responses;
using PerfumeTracker.Server.Features.ChatAgent;
using PerfumeTracker.Server.Features.ChatAgent.Services;
using PerfumeTracker.Server.Features.Perfumes.Services;
using PerfumeTracker.xTests.Fixture;

namespace PerfumeTracker.xTests.Tests;

// Drives the ChatAgent conversation loop offline: canned ResponseResult payloads (parsed with
// ModelReaderWriter) feed the mocked IResponsesClient seam while conversations, messages and
// discussed perfume ids are persisted against the real test database.
public class ChatAgentFixture : DbFixture {
	public Guid DiscussedPerfumeId { get; } = Guid.NewGuid();
	public string DiscussedPerfumeName { get; } = $"ChatAgent Perfume {Guid.NewGuid():N}";

	public override async Task SeedTestData(PerfumeTrackerContext context) {
		context.Perfumes.Add(new Perfume {
			Id = DiscussedPerfumeId,
			UserId = TenantProvider.MockTenantId!.Value,
			House = "ChatAgent House",
			PerfumeName = DiscussedPerfumeName,
			Family = "Amber",
			Parfumeur = "ChatAgent Parfumeur",
			Ml = 100m,
			MlLeft = 50m
		});
		await context.SaveChangesAsync();
	}
}

public class ChatAgentTests(ChatAgentFixture fixture) : IClassFixture<ChatAgentFixture> {
	// ---------- canned Responses API payloads (shapes verified against OpenAI 2.14.0 parsing) ----------

	private static ResponseResult ParseResponse(string json) =>
		ModelReaderWriter.Read<ResponseResult>(BinaryData.FromString(json))!;

	// Text/error payloads are JsonSerializer-serialized before embedding so quotes and control
	// characters in canned strings stay valid JSON.
	private static ResponseResult Completed(string text) {
		var payload = JsonSerializer.Serialize(text);
		return ParseResponse($$"""
			{"id":"resp_ok","object":"response","created_at":1700000000,"status":"completed","model":"gpt-5",
			 "output":[{"type":"message","id":"msg_1","role":"assistant",
				"content":[{"type":"output_text","text":{{payload}},"annotations":[]}]}]}
			""");
	}

	private static ResponseResult CompletedWithToolCall(string callId, string toolName, string arguments) {
		var payload = JsonSerializer.Serialize(arguments);
		return ParseResponse($$"""
			{"id":"resp_tool","object":"response","created_at":1700000000,"status":"completed","model":"gpt-5",
			 "output":[{"type":"function_call","call_id":"{{callId}}","name":"{{toolName}}","arguments":{{payload}}}]}
			""");
	}

	private static ResponseResult Failed(string error) {
		var payload = JsonSerializer.Serialize(error);
		return ParseResponse($$"""
			{"id":"resp_fail","object":"response","created_at":1700000000,"status":"failed","model":"gpt-5",
			 "error":{"code":"server_error","message":{{payload}}},"output":[]}
			""");
	}

	private static ResponseResult Incomplete(string reason) {
		var payload = JsonSerializer.Serialize(reason);
		return ParseResponse($$"""
			{"id":"resp_incomplete","object":"response","created_at":1700000000,"status":"incomplete","model":"gpt-5",
			 "incomplete_details":{"reason":{{payload}}},"output":[]}
			""");
	}

	// ---------- agent construction ----------

	private static Mock<IClientProxy> BuildHubMocks(out Mock<IHubClients> hubClients, out Mock<IHubContext<ChatProgressHub>> hubContext) {
		var clientProxy = new Mock<IClientProxy>();
		_ = clientProxy.Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
			.Returns(Task.CompletedTask);
		hubClients = new Mock<IHubClients>();
		_ = hubClients.Setup(c => c.User(It.IsAny<string>())).Returns(clientProxy.Object);
		hubContext = new Mock<IHubContext<ChatProgressHub>>();
		_ = hubContext.Setup(h => h.Clients).Returns(hubClients.Object);
		return clientProxy;
	}

	private static ChatAgent BuildAgent(
		PerfumeTrackerContext context,
		Mock<IResponsesClient> responsesClient,
		Mock<IChatAgentTools> tools,
		ChatAgentOptions? agentOptions = null) {
		BuildHubMocks(out _, out var hubContext);
		// SystemPromptCache throws when the builder hands back null, so the mock must return a prompt.
		var promptBuilder = new Mock<IChatSystemPromptBuilder>();
		_ = promptBuilder.Setup(b => b.BuildAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync("You are a test perfume assistant.");
		return new ChatAgent(
			context,
			responsesClient.Object,
			Options.Create(new OpenAIOptions()),
			promptBuilder.Object,
			new SystemPromptCache(new MemoryCache(new MemoryCacheOptions())),
			hubContext.Object,
			tools.Object,
			Options.Create(agentOptions ?? new ChatAgentOptions()),
			NullLogger<ChatAgent>.Instance);
	}

	private static Mock<IChatAgentTools> EmptyTools() {
		var tools = new Mock<IChatAgentTools>();
		_ = tools.SetupGet(t => t.Tools).Returns([]);
		return tools;
	}

	// ---------- database helpers ----------

	private PerfumeTrackerContext GetContext() {
		var scope = fixture.Factory.Services.CreateScope();
		return scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
	}

	private async Task<ChatConversation> SeedConversation(string? title = "Seeded", DateTime? titleGeneratedAt = null) {
		using var context = GetContext();
		var conversation = new ChatConversation {
			UserId = fixture.TenantProvider.MockTenantId!.Value,
			Title = title,
			TitleGeneratedAt = titleGeneratedAt
		};
		context.Add(conversation);
		await context.SaveChangesAsync();
		return conversation;
	}

	private async Task AddMessage(Guid conversationId, string role, string content, int index,
		ChatFinishReason? finishReason = null, string? toolCallId = null, string? toolName = null) {
		using var context = GetContext();
		context.Add(new Server.Models.ChatMessage {
			ConversationId = conversationId,
			UserId = fixture.TenantProvider.MockTenantId!.Value,
			Role = role,
			Content = content,
			MessageIndex = index,
			ChatFinishReason = finishReason,
			ToolCallId = toolCallId,
			ToolName = toolName
		});
		await context.SaveChangesAsync();
	}

	private async Task<List<Server.Models.ChatMessage>> LoadMessages(Guid conversationId) {
		using var context = GetContext();
		return await context.Set<Server.Models.ChatMessage>().AsNoTracking()
			.Where(m => m.ConversationId == conversationId)
			.OrderBy(m => m.MessageIndex)
			.ToListAsync();
	}

	// ---------- ChatAsync core flows ----------

	[Fact]
	public async Task ChatAsync_NewConversation_StoresConversationAndBothMessages() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed("You have 12 perfumes."));

		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var result = await agent.ChatAsync(new ChatAgentRequest(null, "How many perfumes do I own?"), TestContext.Current.CancellationToken);

		Assert.Equal("You have 12 perfumes.", result.AssistantMessage);
		using var check = GetContext();
		var conversation = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == result.ConversationId, TestContext.Current.CancellationToken);
		Assert.Equal("You have 12 perfumes.", conversation.Title); // canned title overwrites the placeholder
		Assert.NotNull(conversation.TitleGeneratedAt);
		var messages = await LoadMessages(result.ConversationId);
		Assert.Equal(2, messages.Count);
		Assert.Equal("user", messages[0].Role);
		Assert.Equal(1, messages[0].MessageIndex);
		Assert.Equal("assistant", messages[1].Role);
		Assert.Equal(2, messages[1].MessageIndex);
		Assert.Equal(ChatFinishReason.Stop, messages[1].ChatFinishReason);
	}

	[Fact]
	public async Task ChatAsync_TitleGenerationSkipped_FallsBackToPlaceholderTitle() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.SetupSequence(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed("answer"))
			.ReturnsAsync(Failed("nope"));

		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var result = await agent.ChatAsync(new ChatAgentRequest(null, "Short question"), TestContext.Current.CancellationToken);

		using var check = GetContext();
		var conversation = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == result.ConversationId, TestContext.Current.CancellationToken);
		Assert.Equal("Short question", conversation.Title);
		Assert.Null(conversation.TitleGeneratedAt);
	}

	[Fact]
	public async Task ChatAsync_LongMessage_TruncatesPlaceholderTitle() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.SetupSequence(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed("answer"))
			.ReturnsAsync(Failed("skip title"));

		var longMessage = new string('x', 80);
		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var result = await agent.ChatAsync(new ChatAgentRequest(null, longMessage), TestContext.Current.CancellationToken);

		using var check = GetContext();
		var conversation = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == result.ConversationId, TestContext.Current.CancellationToken);
		Assert.Equal(new string('x', 50) + "...", conversation.Title);
	}

	[Fact]
	public async Task ChatAsync_ToolCallRoundTrip_ExecutesToolAndPersistsToolMessages() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.SetupSequence(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CompletedWithToolCall("call_42", "filter_owned_perfumes", """{"count":3}"""))
			.ReturnsAsync(Completed("Found 3 matches."))
			.ReturnsAsync(Failed("skip title"));

		var tools = new Mock<IChatAgentTools>();
		_ = tools.SetupGet(t => t.Tools).Returns([]);
		_ = tools.Setup(t => t.ExecuteToolCall(It.IsAny<ChatToolCall>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("3 perfumes matched");

		var agent = BuildAgent(context, responsesClient, tools);
		var result = await agent.ChatAsync(new ChatAgentRequest(null, "find niche ambers"), TestContext.Current.CancellationToken);
		Assert.Equal("Found 3 matches.", result.AssistantMessage);

		tools.Verify(t => t.ExecuteToolCall(
			It.Is<ChatToolCall>(call => call.Id == "call_42" && call.FunctionName == "filter_owned_perfumes"),
			It.IsAny<CancellationToken>()), Times.Once);

		var messages = await LoadMessages(result.ConversationId);
		Assert.Equal(4, messages.Count);
		Assert.Equal("user", messages[0].Role);
		Assert.Equal(ChatFinishReason.ToolCalls, messages[1].ChatFinishReason);
		Assert.Contains("\"call_42\"", messages[1].Content);
		Assert.Equal("tool", messages[2].Role);
		Assert.Equal("call_42", messages[2].ToolCallId);
		Assert.Equal("filter_owned_perfumes", messages[2].ToolName);
		Assert.Equal("{\"count\":3}", messages[2].ToolCallArguments);
		Assert.Equal("3 perfumes matched", messages[2].Content);
		Assert.Equal("assistant", messages[3].Role);
		Assert.Equal(ChatFinishReason.Stop, messages[3].ChatFinishReason);
	}

	[Fact]
	public async Task ChatAsync_ToolThrows_ErrorStoredAsToolResult() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.SetupSequence(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CompletedWithToolCall("call_1", "broken_tool", "{}"))
			.ReturnsAsync(Completed("Sorry, that failed."))
			.ReturnsAsync(Failed("skip title"));

		var tools = new Mock<IChatAgentTools>();
		_ = tools.SetupGet(t => t.Tools).Returns([]);
		_ = tools.Setup(t => t.ExecuteToolCall(It.IsAny<ChatToolCall>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new InvalidOperationException("tool exploded"));

		var agent = BuildAgent(context, responsesClient, tools);
		var result = await agent.ChatAsync(new ChatAgentRequest(null, "do the thing"), TestContext.Current.CancellationToken);
		Assert.Equal("Sorry, that failed.", result.AssistantMessage);

		var messages = await LoadMessages(result.ConversationId);
		Assert.Equal("tool", messages[2].Role);
		Assert.Equal("Error: tool exploded", messages[2].Content);
	}

	[Fact]
	public async Task ChatAsync_ModelReportsFailure_ThrowsWithErrorMessage() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Failed("upstream failure"));

		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			agent.ChatAsync(new ChatAgentRequest(null, "hello"), TestContext.Current.CancellationToken));
		Assert.Contains("upstream failure", ex.Message);
	}

	[Fact]
	public async Task ChatAsync_ResponseIncomplete_ThrowsWithReason() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Incomplete("max_output_tokens"));

		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			agent.ChatAsync(new ChatAgentRequest(null, "hello"), TestContext.Current.CancellationToken));
		Assert.Contains("max_output_tokens", ex.Message);
	}

	[Fact]
	public async Task ChatAsync_BlankCompletedText_ThrowsNoAnswer() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed("   "));

		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			agent.ChatAsync(new ChatAgentRequest(null, "hello"), TestContext.Current.CancellationToken));
		Assert.Equal("Chat response contained no answer", ex.Message);
	}

	[Fact]
	public async Task ChatAsync_ToolCallsOnFinalIteration_Throws() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(CompletedWithToolCall("call_1", "filter_owned_perfumes", "{}"));

		var tools = new Mock<IChatAgentTools>();
		_ = tools.SetupGet(t => t.Tools).Returns([]);
		_ = tools.Setup(t => t.ExecuteToolCall(It.IsAny<ChatToolCall>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync("results");

		var agent = BuildAgent(context, responsesClient, tools, new ChatAgentOptions { MaxIterations = 2 });
		var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
			agent.ChatAsync(new ChatAgentRequest(null, "hello"), TestContext.Current.CancellationToken));
		Assert.Equal("Model called tools when tool calls were disabled", ex.Message);
		tools.Verify(t => t.ExecuteToolCall(It.IsAny<ChatToolCall>(), It.IsAny<CancellationToken>()), Times.Once);
	}

	[Fact]
	public async Task ChatAsync_SavesDiscussedPerfumeIds_FromOwnedPerfumeLinksOnly() {
		using var context = GetContext();
		var foreignId = Guid.NewGuid();
		var linkText = $"see [bottle](/perfumes/{fixture.DiscussedPerfumeId}) and /perfumes/{foreignId}";
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.SetupSequence(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed(linkText))
			.ReturnsAsync(Failed("skip"));

		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var result = await agent.ChatAsync(new ChatAgentRequest(null, "tell me about my amber"), TestContext.Current.CancellationToken);

		using var check = GetContext();
		var conversation = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == result.ConversationId, TestContext.Current.CancellationToken);
		Assert.Equal([fixture.DiscussedPerfumeId], conversation.DiscussedPerfumeIds);
	}

	[Fact]
	public async Task ChatAsync_WithoutTenant_ThrowsTenantNotSet() {
		var options = new DbContextOptionsBuilder<PerfumeTrackerContext>()
			.UseNpgsql("Host=localhost;Database=perfumetracker_test;Username=postgres;Password=postgres")
			.Options;
		using var context = new PerfumeTrackerContext(options, null!);
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());

		await Assert.ThrowsAsync<TenantNotSetException>(() =>
			agent.ChatAsync(new ChatAgentRequest(null, "hello"), TestContext.Current.CancellationToken));
	}

	// ---------- conversation reuse and history replay ----------

	[Fact]
	public async Task ChatAsync_ExistingConversation_ReplaysPersistedHistory() {
		var conversation = await SeedConversation("Replay chat");
		var toolCall = ChatToolCall.CreateFunctionToolCall("call_rt", "filter_owned_perfumes", BinaryData.FromString("{\"count\":1}"));
		await AddMessage(conversation.Id, "user", "first question", 1);
		await AddMessage(conversation.Id, "assistant", "first answer", 2, ChatFinishReason.Stop);
		await AddMessage(conversation.Id, "assistant", JsonSerializer.Serialize(new List<ChatToolCall> { toolCall }), 3, ChatFinishReason.ToolCalls);
		await AddMessage(conversation.Id, "tool", "1 perfume matched", 4, toolCallId: "call_rt", toolName: "filter_owned_perfumes");
		await AddMessage(conversation.Id, "tool", "orphan without tool call id", 5);
		await AddMessage(conversation.Id, "assistant", "[]", 6, ChatFinishReason.ToolCalls); // empty tool calls -> plain assistant text

		using var context = GetContext();
		CreateResponseOptions? captured = null;
		var responsesClient = new Mock<IResponsesClient>();
		var canned = new Queue<ResponseResult>([Completed("second answer"), Failed("skip title")]);
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.Callback((CreateResponseOptions o, CancellationToken _) => captured ??= o)
			.ReturnsAsync(() => canned.Dequeue());

		var agent = BuildAgent(context, responsesClient, EmptyTools());
		var result = await agent.ChatAsync(new ChatAgentRequest(conversation.Id, "second question"), TestContext.Current.CancellationToken);
		Assert.Equal(conversation.Id, result.ConversationId);

		// system + 5 replayed (orphan tool row without a ToolCallId is skipped) + new user = 7 input items
		Assert.Equal(7, captured!.InputItems.Count);
		var messages = await LoadMessages(conversation.Id);
		Assert.Equal(8, messages.Count);
		Assert.Equal(7, messages[7].MessageIndex);
		Assert.Equal("assistant", messages[7].Role);
	}

	[Fact]
	public async Task ChatAsync_UnknownConversationId_ThrowsNotFound() {
		using var context = GetContext();
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());
		var missing = Guid.NewGuid();
		var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
			agent.ChatAsync(new ChatAgentRequest(missing, "hello"), TestContext.Current.CancellationToken));
		Assert.Contains(missing.ToString(), ex.Message);
	}

	[Fact]
	public async Task GetConversationAsync_ReturnsConversationWithMessages() {
		var conversation = await SeedConversation("Getter chat");
		await AddMessage(conversation.Id, "user", "hi", 1);

		using var context = GetContext();
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());
		var loaded = await agent.GetConversationAsync(conversation.Id, TestContext.Current.CancellationToken);

		Assert.NotNull(loaded);
		Assert.Equal(conversation.Id, loaded.Id);
		Assert.Single(loaded.Messages);
	}

	[Fact]
	public async Task GetConversationAsync_UnknownId_ReturnsNull() {
		using var context = GetContext();
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());
		Assert.Null(await agent.GetConversationAsync(Guid.NewGuid(), TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task GetUserConversationsAsync_ReturnsTenantConversationsNewestFirst() {
		var first = await SeedConversation("First");
		var second = await SeedConversation("Second");

		using var context = GetContext();
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());
		var conversations = (await agent.GetUserConversationsAsync(TestContext.Current.CancellationToken)).ToList();

		Assert.Contains(first.Id, conversations.Select(c => c.Id));
		Assert.Contains(second.Id, conversations.Select(c => c.Id));
		Assert.All(conversations.Zip(conversations.Skip(1)), pair =>
			Assert.True(pair.First.UpdatedAt >= pair.Second.UpdatedAt));
	}

	// ---------- GenerateAndSaveConversationTitle ----------

	[Fact]
	public async Task GenerateAndSaveConversationTitle_AlreadyGenerated_SkipsModelCall() {
		var conversation = await SeedConversation("Locked", titleGeneratedAt: DateTime.UtcNow);
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		await agent.GenerateAndSaveConversationTitle(conversation, TestContext.Current.CancellationToken);
		responsesClient.Verify(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task GenerateAndSaveConversationTitle_NoVisibleMessages_DoesNothing() {
		var conversation = await SeedConversation();
		await AddMessage(conversation.Id, "tool", "invisible tool output", 1, toolCallId: "call_x");

		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		await agent.GenerateAndSaveConversationTitle(conversation, TestContext.Current.CancellationToken);
		responsesClient.Verify(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()), Times.Never);
	}

	[Fact]
	public async Task GenerateAndSaveConversationTitle_StripsTitlePrefixAndQuotes() {
		var conversation = await SeedConversation();
		await AddMessage(conversation.Id, "user", "what should I buy next?", 1);
		await AddMessage(conversation.Id, "assistant", "try an amber", 2, ChatFinishReason.Stop);

		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		// The server trims surrounding quotes first, then strips the "Title:" prefix, so the
		// realistic model output wraps the whole prefixed title in quotes.
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed("\"Title: Best Amber Blind Buys\""));
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		// The title save mutates the entity passed in, so it must be tracked by the agent's context.
		var tracked = await context.Set<ChatConversation>()
			.SingleAsync(c => c.Id == conversation.Id, TestContext.Current.CancellationToken);
		await agent.GenerateAndSaveConversationTitle(tracked, TestContext.Current.CancellationToken);

		using var check = GetContext();
		var saved = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == conversation.Id, TestContext.Current.CancellationToken);
		Assert.Equal("Best Amber Blind Buys", saved.Title);
		Assert.NotNull(saved.TitleGeneratedAt);
	}

	[Fact]
	public async Task GenerateAndSaveConversationTitle_TruncatesLongTitles() {
		var conversation = await SeedConversation();
		await AddMessage(conversation.Id, "user", "long chat", 1);

		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed(new string('t', 120)));
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		var tracked = await context.Set<ChatConversation>()
			.SingleAsync(c => c.Id == conversation.Id, TestContext.Current.CancellationToken);
		await agent.GenerateAndSaveConversationTitle(tracked, TestContext.Current.CancellationToken);

		using var check = GetContext();
		var saved = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == conversation.Id, TestContext.Current.CancellationToken);
		Assert.Equal(100, saved.Title!.Length);
	}

	[Fact]
	public async Task GenerateAndSaveConversationTitle_FailedResponse_KeepsPlaceholderTitle() {
		var conversation = await SeedConversation("Placeholder");
		await AddMessage(conversation.Id, "user", "hello", 1);

		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Failed("model down"));
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		await agent.GenerateAndSaveConversationTitle(conversation, TestContext.Current.CancellationToken);

		using var check = GetContext();
		var saved = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == conversation.Id, TestContext.Current.CancellationToken);
		Assert.Equal("Placeholder", saved.Title);
		Assert.Null(saved.TitleGeneratedAt);
	}

	[Fact]
	public async Task GenerateAndSaveConversationTitle_BlankTitle_KeepsPlaceholderTitle() {
		var conversation = await SeedConversation("Placeholder");
		await AddMessage(conversation.Id, "user", "hello", 1);

		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed(""));
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		await agent.GenerateAndSaveConversationTitle(conversation, TestContext.Current.CancellationToken);

		using var check = GetContext();
		var saved = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == conversation.Id, TestContext.Current.CancellationToken);
		Assert.Equal("Placeholder", saved.Title);
		Assert.Null(saved.TitleGeneratedAt);
	}

	[Fact]
	public async Task GenerateAndSaveConversationTitle_ModelThrows_SwallowsAndLogs() {
		var conversation = await SeedConversation("Placeholder");
		await AddMessage(conversation.Id, "user", "hello", 1);

		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ThrowsAsync(new HttpRequestException("network down"));
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		await agent.GenerateAndSaveConversationTitle(conversation, TestContext.Current.CancellationToken);

		using var check = GetContext();
		var saved = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == conversation.Id, TestContext.Current.CancellationToken);
		Assert.Null(saved.TitleGeneratedAt);
	}

	// ---------- MediatR handlers and validator ----------

	[Fact]
	public async Task ChatWithAgentHandler_DelegatesToChatAgent() {
		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.SetupSequence(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(Completed("handler answer"))
			.ReturnsAsync(Failed("skip title"));
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		var handler = new ChatWithAgentHandler(agent);
		var result = await handler.Handle(new ChatWithAgentCommand(null, "handler question"), TestContext.Current.CancellationToken);

		Assert.Equal("handler answer", result.AssistantMessage);
	}

	[Fact]
	public async Task GetConversationHandler_MapsVisibleMessagesOnly() {
		var conversation = await SeedConversation("Mapped chat");
		await AddMessage(conversation.Id, "user", "visible question", 1);
		await AddMessage(conversation.Id, "assistant", "hidden tool calls", 2, ChatFinishReason.ToolCalls);
		await AddMessage(conversation.Id, "assistant", "visible answer", 3, ChatFinishReason.Stop);
		await AddMessage(conversation.Id, "tool", "hidden tool output", 4, toolCallId: "call_hidden");

		using var context = GetContext();
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());
		var handler = new GetConversationHandler(agent);

		var result = await handler.Handle(new GetConversationQuery(conversation.Id), TestContext.Current.CancellationToken);

		Assert.NotNull(result.Conversation);
		Assert.Equal("Mapped chat", result.Conversation.Title);
		Assert.Empty(result.Conversation.DiscussedPerfumeIds);
		Assert.Equal(["visible question", "visible answer"], result.Conversation.Messages.Select(m => m.Content));
	}

	[Fact]
	public async Task GetConversationHandler_UnknownConversation_ReturnsNullResult() {
		using var context = GetContext();
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());
		var handler = new GetConversationHandler(agent);

		var result = await handler.Handle(new GetConversationQuery(Guid.NewGuid()), TestContext.Current.CancellationToken);
		Assert.Null(result.Conversation);
	}

	[Fact]
	public async Task GetConversationsHandler_MapsSummaries() {
		var conversation = await SeedConversation("Summary chat");
		using var context = GetContext();
		var agent = BuildAgent(context, new Mock<IResponsesClient>(), EmptyTools());
		var handler = new GetConversationsHandler(agent);

		var summaries = await handler.Handle(new GetConversationsQuery(), TestContext.Current.CancellationToken);

		var summary = Assert.Single(summaries, s => s.Id == conversation.Id);
		Assert.Equal("Summary chat", summary.Title);
	}

	[Fact]
	public void ChatWithAgentCommandValidator_RejectsEmptyOrOversizedMessages() {
		var validator = new ChatWithAgentCommandValidator();

		Assert.False(validator.Validate(new ChatWithAgentCommand(null, "")).IsValid);
		Assert.False(validator.Validate(new ChatWithAgentCommand(null, new string('m', 2001))).IsValid);
		Assert.True(validator.Validate(new ChatWithAgentCommand(null, "valid message")).IsValid);
	}

	// ---------- GenerateConversationTitlesBackgroundService ----------

	[Fact]
	public async Task TitlesBackgroundService_GeneratesPendingTitles_SkipsGeneratedOnes() {
		var pending = await SeedConversation("Pending", titleGeneratedAt: null);
		// Title generation needs a non-empty transcript, so give the pending conversation a message.
		await AddMessage(pending.Id, "user", "pending chat question", 1);
		var done = await SeedConversation("Done before", titleGeneratedAt: DateTime.UtcNow);

		using var context = GetContext();
		var responsesClient = new Mock<IResponsesClient>();
		_ = responsesClient.Setup(c => c.CreateResponseAsync(It.IsAny<CreateResponseOptions>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(() => Completed("Backfill Title"));
		var agent = BuildAgent(context, responsesClient, EmptyTools());

		var services = new ServiceCollection();
		services.AddScoped(_ => context);
		services.AddScoped<IChatAgent>(_ => agent);
		var background = new TitlesTestHost(services.BuildServiceProvider(), NullLogger<GenerateConversationTitlesBackgroundService>.Instance);

		await background.RunAsync(TestContext.Current.CancellationToken);

		using var check = GetContext();
		var savedPending = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == pending.Id, TestContext.Current.CancellationToken);
		Assert.Equal("Backfill Title", savedPending.Title);
		Assert.NotNull(savedPending.TitleGeneratedAt);
		var savedDone = await check.Set<ChatConversation>().AsNoTracking()
			.SingleAsync(c => c.Id == done.Id, TestContext.Current.CancellationToken);
		Assert.Equal("Done before", savedDone.Title);
	}

	// Exposes the protected ExecuteAsync so the background loop can be awaited directly.
	private class TitlesTestHost(IServiceProvider serviceProvider, ILogger<GenerateConversationTitlesBackgroundService> logger)
		: GenerateConversationTitlesBackgroundService(serviceProvider, logger) {
		public Task RunAsync(CancellationToken cancellationToken) => ExecuteAsync(cancellationToken);
	}
}
