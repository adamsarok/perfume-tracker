namespace PerfumeTracker.Server.Exceptions;
public class NotFoundException : Exception {
	public NotFoundException() : base() { }
	public NotFoundException(string message) : base(message) { }
	public NotFoundException(string message, Exception? innerException) : base(message, innerException) { }
	public NotFoundException(string entity, Guid id) : base($"{entity} with ID {id} not found") { }
}