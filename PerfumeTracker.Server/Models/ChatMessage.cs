namespace PerfumeTracker.Server.Models;

public class ChatMessage : UserEntity {
	public Guid ConversationId { get; set; }
	public ChatConversation Conversation { get; set; } = null!;
	public required string Role { get; set; }
	public required string Content { get; set; }
	public string? ToolCallId { get; set; }
	public string? ToolName { get; set; }
	public string? ToolArguments { get; set; }
	public int MessageIndex { get; set; }
}
