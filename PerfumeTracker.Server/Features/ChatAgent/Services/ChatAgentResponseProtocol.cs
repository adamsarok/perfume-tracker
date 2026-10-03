#pragma warning disable OPENAI001 // Responses API is marked experimental in OpenAI 2.13.0.
using OpenAI.Chat;
using OpenAI.Responses;
using System.Text.Json;

namespace PerfumeTracker.Server.Features.ChatAgent.Services;

public static class ChatAgentResponseProtocol {
	public static List<ChatToolCall> DeserializeToolCalls(string content) {
		var data = JsonSerializer.Deserialize<JsonElement>(content);
		var calls = new List<ChatToolCall>();
		if (data.ValueKind != JsonValueKind.Array) return calls;
		foreach (var call in data.EnumerateArray()) {
			var id = call.GetProperty("Id").GetString();
			var name = call.GetProperty("FunctionName").GetString();
			var arguments = call.GetProperty("FunctionArguments");
			// System.Text.Json stores BinaryData as base64; older records may contain a JSON object.
			var binaryArguments = arguments.ValueKind == JsonValueKind.String
				? JsonSerializer.Deserialize<BinaryData>(arguments.GetRawText())!
				: BinaryData.FromString(arguments.GetRawText());
			if (id != null && name != null) calls.Add(ChatToolCall.CreateFunctionToolCall(id, name, binaryArguments));
		}
		return calls;
	}

	public static List<ResponseItem> ConvertHistory(IEnumerable<OpenAI.Chat.ChatMessage> messages) {
		var items = new List<ResponseItem>();
		foreach (var message in messages) {
			var text = string.Concat(message.Content.Select(part => part.Text));
			switch (message) {
				case SystemChatMessage:
					items.Add(ResponseItem.CreateSystemMessageItem(text));
					break;
				case UserChatMessage:
					items.Add(ResponseItem.CreateUserMessageItem(text));
					break;
				case AssistantChatMessage assistant:
					if (!string.IsNullOrEmpty(text)) items.Add(ResponseItem.CreateAssistantMessageItem(text));
					foreach (var call in assistant.ToolCalls) {
						items.Add(ResponseItem.CreateFunctionCallItem(call.Id, call.FunctionName, call.FunctionArguments));
					}
					break;
				case ToolChatMessage tool:
					items.Add(ResponseItem.CreateFunctionCallOutputItem(tool.ToolCallId, text));
					break;
			}
		}
		return items;
	}

	public static CreateResponseOptions CreateOptions(string model, IEnumerable<ResponseItem> history, IEnumerable<ChatTool> tools, bool allowTools) {
		var options = new CreateResponseOptions {
			Model = model,
			StoredOutputEnabled = false,
			ReasoningOptions = new ResponseReasoningOptions { ReasoningEffortLevel = ResponseReasoningEffortLevel.Medium },
			ToolChoice = allowTools ? ResponseToolChoice.CreateAutoChoice() : ResponseToolChoice.CreateNoneChoice()
		};
		// Replay the full output, including encrypted reasoning, on subsequent iterations.
		options.IncludedProperties.Add(IncludedResponseProperty.ReasoningEncryptedContent);
		foreach (var item in history) options.InputItems.Add(item);
		foreach (var tool in tools) {
			options.Tools.Add(ResponseTool.CreateFunctionTool(tool.FunctionName, tool.FunctionParameters,
				tool.FunctionSchemaIsStrict ?? false, tool.FunctionDescription));
		}
		return options;
	}

	public static List<ChatToolCall> GetToolCalls(ResponseResult response) => response.OutputItems
		.OfType<FunctionCallResponseItem>()
		.Select(call => ChatToolCall.CreateFunctionToolCall(call.CallId, call.FunctionName, call.FunctionArguments))
		.ToList();
}
