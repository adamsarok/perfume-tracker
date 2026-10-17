using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using OpenAI.Chat;
using PerfumeTracker.Evals.Cases;
using PerfumeTracker.Evals.Recording;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.Evals.Harness;

/// <summary>
/// Runs one case through the real <see cref="IChatAgent"/> and rebuilds the trajectory from the
/// persisted ChatMessage rows, so scoring sees exactly what a real conversation would record.
/// </summary>
public sealed class ChatAgentRunner(EvalHost host) {
	public async Task<CaseTrajectory> RunAsync(EvalCase evalCase, string promptFingerprint, CancellationToken cancellationToken) {
		var session = new RecordingSession(evalCase.Id);
		host.ResponsesClient.BeginCase(session);

		var conversationId = Guid.Empty;
		string? error = null;
		var stopwatch = Stopwatch.StartNew();
		try {
			using var scope = host.CreateScope();
			var agent = scope.ServiceProvider.GetRequiredService<IChatAgent>();
			var response = await agent.ChatAsync(new ChatAgentRequest(null, evalCase.Message), cancellationToken);
			conversationId = response.ConversationId;
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			throw;
		} catch (Exception ex) {
			error = $"{ex.GetType().Name}: {ex.Message}";
		} finally {
			stopwatch.Stop();
			host.ResponsesClient.EndCase();
		}

		var snapshot = conversationId == Guid.Empty
			? new ConversationSnapshot([], null, [])
			: await host.ReadConversationAsync(conversationId, cancellationToken);

		return Build(evalCase, promptFingerprint, conversationId, snapshot, session, stopwatch.ElapsedMilliseconds, error);
	}

	private static CaseTrajectory Build(
		EvalCase evalCase,
		string promptFingerprint,
		Guid conversationId,
		ConversationSnapshot snapshot,
		RecordingSession session,
		long durationMs,
		string? error) {
		var calls = new List<ToolCallTrace>();
		var resultsByCallId = new Dictionary<string, string>(StringComparer.Ordinal);
		var userMessage = evalCase.Message;
		var finalAnswer = string.Empty;

		foreach (var message in snapshot.Messages) {
			switch (message.Role) {
				case "user":
					userMessage = message.Content;
					break;
				case "assistant" when message.ChatFinishReason == ChatFinishReason.ToolCalls:
					foreach (var call in ChatAgentResponseProtocol.DeserializeToolCalls(message.Content)) {
						calls.Add(new ToolCallTrace(call.Id, call.FunctionName, call.FunctionArguments.ToString(), null));
					}
					break;
				case "assistant":
					finalAnswer = message.Content;
					break;
				case "tool" when !string.IsNullOrEmpty(message.ToolCallId):
					resultsByCallId[message.ToolCallId] = message.Content;
					break;
			}
		}

		calls = calls
			.Select(call => call with { Result = resultsByCallId.GetValueOrDefault(call.Id) })
			.ToList();

		return new CaseTrajectory {
			CaseId = evalCase.Id,
			UserMessage = userMessage,
			FinalAnswer = finalAnswer,
			Title = snapshot.Title,
			ConversationId = conversationId,
			ChatModelCalls = session.ChatModelCalls,
			TitleModelCalls = session.TitleModelCalls,
			InputTokens = session.InputTokens,
			OutputTokens = session.OutputTokens,
			DurationMs = durationMs,
			PromptFingerprint = promptFingerprint,
			ToolCalls = calls,
			DiscussedPerfumeIds = snapshot.DiscussedPerfumeIds,
			Error = error
		};
	}
}
