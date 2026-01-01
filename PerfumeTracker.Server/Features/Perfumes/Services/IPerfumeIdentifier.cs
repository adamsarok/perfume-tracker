namespace PerfumeTracker.Server.Features.Perfumes.Services;

public record IdentifiedPerfume(
	string House,
	string PerfumeName,
	string Family,
	IEnumerable<string> Notes,
	double ConfidenceScore);

public interface IPerfumeIdentifier {
	Task<IdentifiedPerfume> GetIdentifiedPerfumeAsync(string house, string perfumeName, CancellationToken cancellationToken);
}
