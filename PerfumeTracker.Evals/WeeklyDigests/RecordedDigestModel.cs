using System.Text.Json;
using System.Text.Json.Nodes;
using PerfumeTracker.Evals.Recording;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.Evals.WeeklyDigests;

public sealed record DigestModelRecording(string RequestHash, JsonObject Response);

public sealed class RecordedDigestModel(EvalMode mode, string root, string caseId, string fixtureManifest,
	IDigestModel? live, DigestEvalTrajectory trajectory) : IDigestModel {
	private const string FailureStatus = "digest_transport_failure";
	public static bool IsFailure(JsonElement response) => response.TryGetProperty("status", out var status) && status.GetString() == FailureStatus;
	public static string Hash(JsonNode value) => Fingerprints.Compute(Canonical(value));
	private static string Canonical(JsonNode? value) => value switch {
		JsonObject obj => "{" + string.Join(",", obj.OrderBy(p => p.Key, StringComparer.Ordinal)
			.Select(p => JsonSerializer.Serialize(p.Key) + ":" + Canonical(p.Value))) + "}",
		JsonArray array => "[" + string.Join(",", array.Select(Canonical)) + "]",
		_ => value?.ToJsonString() ?? "null"
	};

	public async Task<JsonObject> Generate(JsonObject request, CancellationToken ct) {
		var index = trajectory.ModelCalls.Count;
		var hash = Hash(request);
		if (trajectory.Fingerprint.Length == 0) trajectory.Fingerprint = Fingerprints.Compute(fixtureManifest + "\n" + hash);
		// File names use a hash of the case ID so custom datasets cannot escape the recording root.
		var path = Path.Combine(root, trajectory.Fingerprint, Fingerprints.Compute(caseId), $"{index:00}.json");
		JsonObject response;
		if (mode == EvalMode.Replay) {
			if (!File.Exists(path)) throw new InvalidDataException($"Missing digest recording for '{caseId}' call {index}, fingerprint {trajectory.Fingerprint}. Run --mode record.");
			var recording = JsonSerializer.Deserialize<DigestModelRecording>(await File.ReadAllTextAsync(path, ct), DigestJson.Options)
				?? throw new InvalidDataException("Invalid digest recording.");
			if (recording.RequestHash != hash) throw new InvalidDataException($"Digest recording request mismatch for '{caseId}' call {index}. Run --mode record.");
			response = recording.Response;
		} else {
			try { response = await (live ?? throw new InvalidOperationException("No digest API client configured.")).Generate(request, ct); }
			catch (Exception ex) when (!ct.IsCancellationRequested && DigestGenerationRetry.IsTransient(ex)) {
				response = new JsonObject { ["status"] = FailureStatus, ["errorType"] = ex.GetType().Name,
					["statusCode"] = (ex as DigestApiException)?.StatusCode,
					["retryAfterSeconds"] = (ex as DigestApiException)?.RetryAfter?.TotalSeconds };
			}
			if (mode == EvalMode.Record) {
				Directory.CreateDirectory(Path.GetDirectoryName(path)!);
				await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new DigestModelRecording(hash, response), DigestJson.Options), ct);
			}
		}
		trajectory.ModelCalls.Add(new(index, request["tool_choice"]?.GetValue<string>() == "none" ? "finalization" : "exploration",
			hash, JsonSerializer.SerializeToElement(request), JsonSerializer.SerializeToElement(response)));
		if (response["status"]?.GetValue<string>() == FailureStatus) {
			var seconds = response["retryAfterSeconds"]?.GetValue<double>();
			throw response["errorType"]?.GetValue<string>() switch {
				// Replay retains the original header in the trace but must not actually wait for a live rate-limit window.
				nameof(DigestApiException) => new DigestApiException(response["statusCode"]!.GetValue<int>(), mode == EvalMode.Replay ? TimeSpan.Zero : seconds == null ? null : TimeSpan.FromSeconds(seconds.Value)),
				nameof(TimeoutException) => new TimeoutException("Recorded digest request timed out."),
				nameof(TaskCanceledException) => new TaskCanceledException("Recorded digest request timed out."),
				_ => new HttpRequestException("Recorded digest transport failure.")
			};
		}
		return response;
	}
}
