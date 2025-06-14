namespace PerfumeTracker.Server.Exceptions {
	public class MappingException : BadRequestException {
		public MappingException() : base() { }
		public MappingException(string message) : base(message) { }
	}
}
