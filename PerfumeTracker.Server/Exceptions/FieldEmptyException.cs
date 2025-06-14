namespace PerfumeTracker.Server.Exceptions;
public class FieldEmptyException : BadRequestException {
	public FieldEmptyException() : base() { }
	public FieldEmptyException(string fieldName) : base($"{fieldName} is empty") { }
}
