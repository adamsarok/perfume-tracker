using System.Text.Json;

namespace PerfumeTracker.Server.Models;

public class OutboxMessage : Entity {
	public static OutboxMessage From<T>(T payload) {
		return new OutboxMessage() {
			EventType = payload.GetType().AssemblyQualifiedName,
			Payload = JsonSerializer.Serialize(payload),
			UserId = PerfumeTrackerContext.DEFAULT_USERID,
		};
	}
	public int Id { get; set; }
	public string EventType { get; set; }
	public string Payload { get; set; }
	public DateTime? ProcessedAt { get; set; }
}