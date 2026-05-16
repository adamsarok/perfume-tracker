using System.Diagnostics;

namespace PerfumeTracker.Server.Models;

public class OutboxMessage : UserEntity {
	public static OutboxMessage From<T>(T payload) where T : notnull {
		if (payload is null) throw new ArgumentNullException(nameof(payload));
		var activity = Activity.Current;
		var aqn = payload.GetType().AssemblyQualifiedName ?? throw new InvalidOperationException("Type.AssemblyQualifiedName is null");
		return new OutboxMessage {
			EventType = aqn,
			Payload = JsonSerializer.Serialize(payload),
			TraceId = activity?.TraceId.ToString(),
			SpanId = activity?.SpanId.ToString(),
			TraceFlags = activity?.ActivityTraceFlags.ToString()
		};
	}
	public string EventType { get; set; } = null!;
	public string Payload { get; set; } = null!;
	public DateTime? ProcessedAt { get; set; }
	public int TryCount { get; set; } = 0;
	public DateTime? FailedAt { get; set; }
	public string? LastError { get; set; } = null;
	public string? TraceId { get; set; }
	public string? SpanId { get; set; }
	public string? TraceFlags { get; set; }
}