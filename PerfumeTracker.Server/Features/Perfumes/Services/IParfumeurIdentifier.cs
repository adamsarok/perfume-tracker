namespace PerfumeTracker.Server.Features.Perfumes.Services;

public record IdentifiedParfumeur(
	string House,
	string PerfumeName,
	string Parfumeur,
	double ConfidenceScore);

public interface IParfumeurIdentifier {
	Task<IdentifiedParfumeur> GetIdentifiedParfumeurAsync(string house, string perfumeName, Guid userId, CancellationToken cancellationToken);
}
