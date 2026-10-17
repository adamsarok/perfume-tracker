using System.ClientModel.Primitives;
using OpenAI.Responses;

namespace PerfumeTracker.Evals.Recording;

/// <summary>
/// Stores model responses per prompt fingerprint + case + call index. The fingerprint changes
/// whenever the system prompt or the tool schemas change, which is what invalidates recordings.
/// </summary>
public sealed class EvalRecordingStore(string root) {
	public string Root { get; } = root;

	public string DirectoryFor(string fingerprint, string caseId) => Path.Combine(Root, fingerprint, Sanitize(caseId));

	public string FileFor(string fingerprint, string caseId, int callIndex) =>
		Path.Combine(DirectoryFor(fingerprint, caseId), $"{callIndex:00}.json");

	public async Task SaveAsync(string fingerprint, string caseId, int callIndex, ResponseResult response, CancellationToken cancellationToken) {
		var path = FileFor(fingerprint, caseId, callIndex);
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		var json = ModelReaderWriter.Write(response, ModelReaderWriterOptions.Json);
		await File.WriteAllTextAsync(path, json.ToString(), cancellationToken);
	}

	public async Task<ResponseResult?> TryLoadAsync(string fingerprint, string caseId, int callIndex, CancellationToken cancellationToken) {
		var path = FileFor(fingerprint, caseId, callIndex);
		if (!File.Exists(path)) return null;
		var json = await File.ReadAllTextAsync(path, cancellationToken);
		return ModelReaderWriter.Read<ResponseResult>(BinaryData.FromString(json));
	}

	public int RecordedResponseCount(string fingerprint, string caseId) {
		var directory = DirectoryFor(fingerprint, caseId);
		return Directory.Exists(directory) ? Directory.GetFiles(directory, "*.json").Length : 0;
	}

	private static string Sanitize(string value) =>
		string.Concat(value.Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '_'));
}
