namespace PerfumeTracker.Server.Exceptions;

public class MappingException : BadRequestException {
	public MappingException() : base() { }
	public MappingException(string message) : base(message) { }
	public MappingException(string message, Exception? innerException) : base(message, innerException) { }
}
