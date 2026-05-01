namespace PerfumeTracker.Server.Features.Tags.Services;

public record IdentifiedTagNoteGroup(
	string TagName,
	string NoteGroup,
	double ConfidenceScore);

public record IdentifiedTagNoteGroups(
	IEnumerable<IdentifiedTagNoteGroup> Tags);

public interface ITagNoteGroupIdentifier {
	Task<IdentifiedTagNoteGroups> GetIdentifiedTagNoteGroupsAsync(
		IReadOnlyCollection<string> tagNames,
		Guid userId,
		CancellationToken cancellationToken);
}
