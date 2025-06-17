namespace PerfumeTracker.Server.Exceptions;

public class TenantNotSetException : BadRequestException {
	public TenantNotSetException() : base("Tenant not set") { }
}
