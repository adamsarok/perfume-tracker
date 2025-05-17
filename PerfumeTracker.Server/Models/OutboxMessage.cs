using System.Text.Json;

namespace PerfumeTracker.Server.Models;

public class OutboxMessage {
	public static OutboxMessage From<T>(T payload) {
		return new OutboxMessage() {
			CreatedAt = DateTime.UtcNow,
			EventType = payload.GetType().AssemblyQualifiedName,
			Payload = JsonSerializer.Serialize(payload)
		};
	}
	public int Id { get; set; }
	public string EventType { get; set; }
	public string Payload { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime? ProcessedAt { get; set; }
}