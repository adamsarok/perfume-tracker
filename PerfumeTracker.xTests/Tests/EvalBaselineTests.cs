using System.Text.Json;
using PerfumeTracker.Evals.Reporting;

namespace PerfumeTracker.xTests.Tests;

public class EvalBaselineTests {
	[Theory]
	[InlineData("chat")]
	[InlineData("weekly-digest")]
	public async Task Snapshot_PreservesRegressionAcrossSamePathAndIncrementalWrites(string suite) {
		var directory = Path.Combine(Path.GetTempPath(), "eval-baseline-tests", Guid.NewGuid().ToString());
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, "results.json");
		var ct = TestContext.Current.CancellationToken;
		try {
			await ReportWriter.WriteAsync(new EvalReport { Suite = suite, Cases = [Result("first", CaseStatus.Passed), Result("second", CaseStatus.Passed)] }, directory, [], ct);
			var baseline = BaselineComparison.Load(path);
			// Digest progress reporting overwrites this file before the second case finishes.
			var partial = new List<CaseResult> { Result("first", CaseStatus.Passed) };
			await ReportWriter.WriteAsync(new EvalReport { Suite = suite, Cases = partial }, directory, baseline.Compare(partial), ct);
			var current = new List<CaseResult> { Result("first", CaseStatus.Passed), Result("second", CaseStatus.Failed) };
			var comparison = baseline.Compare(current);
			var markdown = await ReportWriter.WriteAsync(new EvalReport { Suite = suite, Cases = current }, directory, comparison, ct);
			Assert.True(BaselineComparison.HasRegression(comparison));
			Assert.Contains("REGRESSED: second (Passed -> Failed)", markdown);
			Assert.Contains("REGRESSED: second (Passed -> Failed)", await File.ReadAllTextAsync(Path.Combine(directory, "summary.md"), ct));
			using var report = JsonDocument.Parse(await File.ReadAllTextAsync(path, ct));
			Assert.Equal("Failed", report.RootElement.GetProperty("cases")[1].GetProperty("status").GetString());
		} finally { Directory.Delete(directory, recursive: true); }
	}

	[Fact]
	public async Task MissingBaseline_DoesNotBecomeTheNewOutputReport() {
		var directory = Path.Combine(Path.GetTempPath(), "eval-baseline-tests", Guid.NewGuid().ToString());
		try {
			var baseline = BaselineComparison.Load(Path.Combine(directory, "results.json"));
			var results = new List<CaseResult> { Result("case", CaseStatus.Passed) };
			await ReportWriter.WriteAsync(new EvalReport { Cases = results }, directory, [], TestContext.Current.CancellationToken);
			Assert.StartsWith("baseline file not found:", Assert.Single(baseline.Compare(results)));
		} finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
	}

	[Fact]
	public async Task Snapshot_PreservesFixedNewSkippedAndUnchangedSemantics() {
		var directory = Path.Combine(Path.GetTempPath(), "eval-baseline-tests", Guid.NewGuid().ToString());
		try {
			var previous = new List<CaseResult> { Result("fixed", CaseStatus.Failed), Result("skipped", CaseStatus.Passed), Result("removed", CaseStatus.Passed) };
			await ReportWriter.WriteAsync(new EvalReport { Cases = previous }, directory, [], TestContext.Current.CancellationToken);
			var snapshot = BaselineComparison.Load(Path.Combine(directory, "results.json"));
			var comparison = snapshot.Compare([Result("fixed", CaseStatus.Passed), Result("skipped", CaseStatus.Skipped), Result("new", CaseStatus.Passed)]);
			Assert.Contains("fixed: fixed (Failed -> Passed)", comparison);
			Assert.Contains("new: new (Passed)", comparison);
			Assert.Contains("missing from this run: removed", comparison);
			Assert.False(BaselineComparison.HasRegression(comparison));
			Assert.Equal("no change versus baseline", Assert.Single(snapshot.Compare(previous)));
		} finally { if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true); }
	}

	private static CaseResult Result(string id, CaseStatus status) => new() { CaseId = id, Category = "test", Status = status };
}
