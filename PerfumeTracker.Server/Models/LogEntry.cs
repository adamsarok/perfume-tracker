namespace PerfumeTracker.Server.Models;

[Keyless]
public class LogEntry {
	public string Message {  get; set; } = string.Empty;
	public int Level { get; set; }
	public DateTime Timestamp { get; set; }
	public string? Exception { get; set; }
	public string? Properties { get; set; }
}