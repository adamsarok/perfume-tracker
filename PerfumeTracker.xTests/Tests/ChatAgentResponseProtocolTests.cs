#pragma warning disable OPENAI001 // Responses API is marked experimental in OpenAI 2.13.0.
using System.ClientModel;
using System.ClientModel.Primitives;
using System.Net;
using System.Text;
using System.Text.Json;
using OpenAI.Chat;
using OpenAI.Responses;
using PerfumeTracker.Server.Features.ChatAgent.Services;

namespace PerfumeTracker.xTests.Tests;

public class ChatAgentResponseProtocolTests {
	[Fact]
	public void DeserializeToolCalls_RestoresPersistedBinaryArguments() {
		var original = ChatToolCall.CreateFunctionToolCall("call_old", "search", BinaryData.FromString("{\"query\":\"vanilla\"}"));
		var calls = ChatAgentResponseProtocol.DeserializeToolCalls(JsonSerializer.Serialize(new[] { original }));
		var restored = Assert.Single(calls);
		Assert.Equal(original.Id, restored.Id);
		Assert.Equal(original.FunctionName, restored.FunctionName);
		Assert.Equal(original.FunctionArguments.ToString(), restored.FunctionArguments.ToString());
		var input = ChatAgentResponseProtocol.ConvertHistory([new AssistantChatMessage(calls)]);
		Assert.Equal("{\"query\":\"vanilla\"}", Assert.IsType<FunctionCallResponseItem>(Assert.Single(input)).FunctionArguments.ToString());
	}

	[Fact]
	public void DeserializeToolCalls_AcceptsLegacyJsonArguments() {
		var calls = ChatAgentResponseProtocol.DeserializeToolCalls("""
			[{"Id":"call_old","Kind":0,"FunctionName":"search","FunctionArguments":{"query":"vanilla"}}]
			""");
		Assert.Equal("{\"query\":\"vanilla\"}", Assert.Single(calls).FunctionArguments.ToString());
	}

	[Fact]
	public void ConvertHistory_PreservesMessagesAndMultipleToolCallIds() {
		var history = ChatAgentResponseProtocol.ConvertHistory([
			new SystemChatMessage("Perfume assistant"),
			new UserChatMessage("Find vanilla perfumes"),
			new AssistantChatMessage([
				ChatToolCall.CreateFunctionToolCall("call_one", "search", BinaryData.FromString("{\"query\":\"vanilla\"}")),
				ChatToolCall.CreateFunctionToolCall("call_two", "ownership", BinaryData.FromString("{}"))
			]),
			new ToolChatMessage("call_one", "Found perfume"),
			new ToolChatMessage("call_two", "Owned"),
			new AssistantChatMessage("Try your vanilla perfume")
		]);

		Assert.Equal(7, history.Count);
		Assert.IsAssignableFrom<MessageResponseItem>(history[0]);
		Assert.IsAssignableFrom<MessageResponseItem>(history[1]);
		var firstCall = Assert.IsType<FunctionCallResponseItem>(history[2]);
		Assert.Equal("call_one", firstCall.CallId);
		Assert.Equal("{\"query\":\"vanilla\"}", firstCall.FunctionArguments.ToString());
		Assert.Equal("call_two", Assert.IsType<FunctionCallResponseItem>(history[3]).CallId);
		Assert.Equal("call_one", Assert.IsType<FunctionCallOutputResponseItem>(history[4]).CallId);
		Assert.Equal("call_two", Assert.IsType<FunctionCallOutputResponseItem>(history[5]).CallId);
		Assert.IsAssignableFrom<MessageResponseItem>(history[6]);
	}

	[Theory]
	[InlineData("gpt-6.1-sol")]
	[InlineData("gpt-6-luna")]
	public async Task Responses_ResubmitsReasoningAndToolOutputsAndDisablesFinalToolCalls(string model) {
		using var handler = new ResponsesHandler();
		using var httpClient = new HttpClient(handler);
		var client = new ResponsesClient(new ApiKeyCredential("test-key"), new ResponsesClientOptions {
			Transport = new HttpClientPipelineTransport(httpClient)
		});
		List<ResponseItem> history = [ResponseItem.CreateUserMessageItem("Recommend a perfume")];
		ChatTool[] tools = [ChatTool.CreateFunctionTool("ownership", "Check ownership", BinaryData.FromString("{\"type\":\"object\",\"properties\":{}}"))];
		var response = (await client.CreateResponseAsync(
			ChatAgentResponseProtocol.CreateOptions(model, history, tools, allowTools: true), TestContext.Current.CancellationToken)).Value;
		Assert.Equal(ResponseStatus.Completed, response.Status);
		history.AddRange(response.OutputItems);
		var calls = ChatAgentResponseProtocol.GetToolCalls(response);
		Assert.Equal(2, calls.Count);
		Assert.Equal("call_one", calls[0].Id); // Use call_id, never the output item's id.
		Assert.Equal("call_two", calls[1].Id);
		foreach (var call in calls) history.Add(ResponseItem.CreateFunctionCallOutputItem(call.Id, "Not owned"));

		var finalResponse = (await client.CreateResponseAsync(
			ChatAgentResponseProtocol.CreateOptions(model, history, tools, allowTools: false), TestContext.Current.CancellationToken)).Value;
		Assert.Empty(ChatAgentResponseProtocol.GetToolCalls(finalResponse));
		Assert.Equal("Try this perfume.", finalResponse.GetOutputText());

		var initial = handler.Requests[0];
		Assert.Equal("/v1/responses", handler.Paths[0]);
		Assert.Equal(model, initial.GetProperty("model").GetString());
		Assert.False(initial.GetProperty("store").GetBoolean());
		Assert.Equal("medium", initial.GetProperty("reasoning").GetProperty("effort").GetString());
		Assert.Contains(initial.GetProperty("include").EnumerateArray(), value => value.GetString() == "reasoning.encrypted_content");
		Assert.Equal("auto", initial.GetProperty("tool_choice").GetString());
		Assert.False(initial.GetProperty("tools")[0].GetProperty("strict").GetBoolean());
		var final = handler.Requests[1];
		Assert.Equal("none", final.GetProperty("tool_choice").GetString());
		var input = final.GetProperty("input");
		Assert.Equal("reasoning", input[1].GetProperty("type").GetString());
		Assert.Equal("encrypted-reasoning", input[1].GetProperty("encrypted_content").GetString());
		Assert.Equal("call_one", input[4].GetProperty("call_id").GetString());
		Assert.Equal("call_two", input[5].GetProperty("call_id").GetString());
		Assert.Equal("function_call_output", input[4].GetProperty("type").GetString());
	}

	private sealed class ResponsesHandler : HttpMessageHandler {
		public List<JsonElement> Requests { get; } = [];
		public List<string> Paths { get; } = [];
		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
			Paths.Add(request.RequestUri!.AbsolutePath);
			Requests.Add(JsonSerializer.Deserialize<JsonElement>(await request.Content!.ReadAsStringAsync(cancellationToken)));
			var output = Requests.Count == 1
				? """
				  [{"type":"reasoning","id":"rs_one","summary":[],"encrypted_content":"encrypted-reasoning"},
				   {"type":"function_call","id":"fc_one","call_id":"call_one","name":"ownership","arguments":"{}","status":"completed"},
				   {"type":"function_call","id":"fc_two","call_id":"call_two","name":"ownership","arguments":"{}","status":"completed"}]
				  """
				: """
				  [{"type":"message","id":"msg_one","role":"assistant","status":"completed","content":[{"type":"output_text","text":"Try this perfume.","annotations":[]}]}]
				  """;
			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StringContent("""
					{"id":"resp_test","object":"response","created_at":0,"status":"completed","model":"test","output":
					""" + output + "}", Encoding.UTF8, "application/json")
			};
		}
	}
}
