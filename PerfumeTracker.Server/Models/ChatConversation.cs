namespace PerfumeTracker.Server.Models;

public class ChatConversation : UserEntity {
	public string? Title { get; set; }
	public ICollection<ChatMessage> Messages { get; set; } = [];
}
