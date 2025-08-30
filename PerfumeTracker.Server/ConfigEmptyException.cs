namespace PerfumeTracker.Server;
public class ConfigEmptyException : Exception {
	public ConfigEmptyException() : base() { }
	public ConfigEmptyException(string msg) : base(msg) { }
	public ConfigEmptyException(string msg, Exception? innerException) : base(msg, innerException) { }
}