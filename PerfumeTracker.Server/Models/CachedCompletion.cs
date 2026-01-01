namespace PerfumeTracker.Server.Models;

public class CachedCompletion : UserEntity {
	public required CompletionTypes CompletionType { get; set; }
	public required string Prompt { get; set; }
	public required string Response { get; set; }
	public enum CompletionTypes {
		MoodOrOccasionRecommendation,
		IdentifyPerfume
	}
}
