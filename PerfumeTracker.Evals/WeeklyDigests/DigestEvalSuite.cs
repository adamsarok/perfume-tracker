using System.Diagnostics;
using Microsoft.Extensions.Options;
using PerfumeTracker.Evals.Recording;
using PerfumeTracker.Evals.Reporting;
using PerfumeTracker.Evals.Scoring;
using PerfumeTracker.Server.Features.WeeklyDigests;

namespace PerfumeTracker.Evals.WeeklyDigests;

public static class DigestEvalSuite {
	public static async Task<int> RunAsync(EvalRunOptions options, CancellationToken ct) {
		var cases = await DigestEvalCase.LoadAsync(EvalPaths.Resolve(options.CaseFile ?? "Cases/weekly-digest.cases.json"), options.Filter, ct);
		var recordings = EvalPaths.Resolve(options.RecordingsDirectory ?? "Recordings/weekly-digest");
		var output = EvalPaths.Resolve(options.OutputDirectory ?? "results/weekly-digest");
		var baseline = options.BaselinePath == null ? null : EvalPaths.Resolve(options.BaselinePath);
		using var host = new DigestEvalHost(options);
		await host.InitializeAsync(ct);
		using var judgeHttp = new HttpClient { BaseAddress = new Uri("https://api.openai.com/v1/"), Timeout = TimeSpan.FromMinutes(5) };
		// Replay is key-free by default. An explicit judge-model opts into paid judging during replay.
		var judgeEnabled = !options.SkipJudge && (options.Mode != EvalMode.Replay || options.JudgeModel != null);
		if (judgeEnabled && string.IsNullOrWhiteSpace(host.ApiKey)) throw new InvalidOperationException("A requested digest judge requires OpenAI:ApiKey.");
		var judgeModel = options.JudgeModel ?? host.Model;
		IDigestModel? judge = judgeEnabled ? new DigestModel(judgeHttp, Options.Create(new OpenAIOptions { ApiKey = host.ApiKey! })) : null;
		var started = DateTime.UtcNow;
		var watch = Stopwatch.StartNew();
		var results = new List<CaseResult>();
		Console.WriteLine($"PerfumeTracker weekly digest eval | mode={options.Mode} | {cases.Count} case(s) | model={host.Model}");
		foreach (var evalCase in cases) {
			ct.ThrowIfCancellationRequested();
			Console.WriteLine($"  RUN    {evalCase.Id}");
			var trajectory = await host.RunAsync(evalCase, recordings, ct);
			var scores = DigestEvalScoring.Score(evalCase, trajectory);
			scores.Add(judge == null ? ScoreResult.Skipped("digest-judge", "Judge disabled; deterministic replay checks still run.")
				: await DigestEvalScoring.JudgeAsync(judgeModel, judge, evalCase, trajectory, ct));
			var status = trajectory.Error != null ? CaseStatus.Error : scores.Any(s => s.Outcome == ScoreOutcome.Fail) ? CaseStatus.Failed : CaseStatus.Passed;
			results.Add(new CaseResult { CaseId = evalCase.Id, Category = "weekly-digest", Status = status, Scores = scores,
				DigestTrajectory = trajectory, DigestMetrics = DigestEvalScoring.Metrics(evalCase, trajectory), Note = trajectory.Error });
			Console.WriteLine($"  {status.ToString().ToUpperInvariant(),-6} {evalCase.Id} [{trajectory.ModelCalls.Count} calls, {trajectory.TokensUsed} tokens]");
			foreach (var score in scores.Where(s => s.Outcome == ScoreOutcome.Fail)) Console.WriteLine($"         {score.Scorer}: {score.Detail}");
			// Preserve progress even if a later paid model call is interrupted.
			await WriteReport();
		}
		var report = await WriteReport();
		Console.WriteLine($"Passed {report.Passed} | Failed {report.Failed} | Errored {report.Errored}");
		Console.WriteLine($"Report: {Path.Combine(output, "summary.md")}");
		if (baseline != null && BaselineComparison.Compare(baseline, results).Any(line => line.Contains("REGRESSED", StringComparison.Ordinal))) return 1;
		return options.FailOnAnyFailure && (report.Failed > 0 || report.Errored > 0) ? 1 : 0;

		async Task<EvalReport> WriteReport() {
			var report = new EvalReport { Suite = "weekly-digest", Mode = options.Mode.ToString(), Model = host.Model,
				JudgeModel = judgeEnabled ? judgeModel : null, StartedAt = started, DurationMs = watch.ElapsedMilliseconds,
				PromptFingerprint = Fingerprints.Compute(string.Join("\n", results.Select(r => r.DigestTrajectory!.Fingerprint))),
				Passed = results.Count(r => r.Status == CaseStatus.Passed), Failed = results.Count(r => r.Status == CaseStatus.Failed),
				Errored = results.Count(r => r.Status == CaseStatus.Error), InputTokens = results.Sum(r => r.DigestTrajectory!.InputTokens),
				OutputTokens = results.Sum(r => r.DigestTrajectory!.OutputTokens), Cases = results.ToList() };
			await ReportWriter.WriteAsync(report, output, baseline, ct);
			return report;
		}
	}
}
