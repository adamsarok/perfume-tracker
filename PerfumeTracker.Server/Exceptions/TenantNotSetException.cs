namespace PerfumeTracker.Server.Exceptions;

public class TenantNotSetException : BadRequestException {
	public TenantNotSetException() : base() { }
	public TenantNotSetException(string message) : base(message) { }
	public TenantNotSetException(string message, Exception? innerException) : base(message, innerException) { }
}
