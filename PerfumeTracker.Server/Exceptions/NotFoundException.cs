namespace PerfumeTracker.Server.Exceptions {
	public class NotFoundException : Exception {
		public NotFoundException() : base() { }
		public NotFoundException(string entity, Guid id) : base($"{entity} with ID {id} not found") { }
	}
}
