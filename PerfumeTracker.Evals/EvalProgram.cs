using System.Diagnostics;
using OpenAI;
using PerfumeTracker.Evals;
using PerfumeTracker.Evals.Cases;
using PerfumeTracker.Evals.Harness;
using PerfumeTracker.Evals.Recording;
using PerfumeTracker.Evals.Reporting;
using PerfumeTracker.Evals.Scoring;
using PerfumeTracker.Server.Features.ChatAgent.Services;

internal static class EvalProgram {
	private static async Task<int> Main(string[] args) {
		if (args.Contains("--help") || args.Contains("-h")) {
			Console.WriteLine(EvalRunOptions.Usage);
			return 0;
		}

		EvalRunOptions options;
		try {
			options = EvalRunOptions.Parse(args);
		} catch (ArgumentException ex) {
			Console.Error.WriteLine(ex.Message);
			Console.Error.WriteLine();
			Console.Error.WriteLine(EvalRunOptions.Usage);
			return 2;
		}

		using var cancellation = new CancellationTokenSource();
		Console.CancelKeyPress += (_, eventArgs) => {
			eventArgs.Cancel = true;
			cancellation.Cancel();
		};
		var cancellationToken = cancellation.Token;
		if (options.Suite == "weekly-digest") {
			try { return await PerfumeTracker.Evals.WeeklyDigests.DigestEvalSuite.RunAsync(options, cancellationToken); }
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return 130; }
			catch (Exception ex) { Console.Error.WriteLine($"Could not run digest evals: {ex.Message}"); return 2; }
		}

		var caseFile = EvalPaths.Resolve(options.CaseFile ?? EvalPaths.DefaultCaseFile);
		var recordingsDirectory = EvalPaths.Resolve(options.RecordingsDirectory ?? EvalPaths.DefaultRecordingsDirectory);
		var outputDirectory = EvalPaths.Resolve(options.OutputDirectory ?? EvalPaths.DefaultResultsDirectory);
		var baselinePath = string.IsNullOrWhiteSpace(options.BaselinePath) ? null : EvalPaths.Resolve(options.BaselinePath);

		List<EvalCase> cases;
		try {
			cases = await EvalCaseLoader.LoadAsync(caseFile, options.Filter, cancellationToken);
		} catch (Exception ex) {
			Console.Error.WriteLine($"Could not load cases: {ex.Message}");
			return 2;
		}

		Console.WriteLine($"PerfumeTracker chat agent eval | mode={options.Mode.ToString().ToLowerInvariant()} | {cases.Count} case(s)");
		Console.WriteLine($"Cases: {caseFile}");
		Console.WriteLine($"Recordings: {recordingsDirectory}");

		var startedAt = DateTime.UtcNow;
		var runStopwatch = Stopwatch.StartNew();

		await using var host = new EvalHost(options, new EvalRecordingStore(recordingsDirectory));
		try {
			await host.InitializeAsync(cancellationToken);
		} catch (Exception ex) {
			Console.Error.WriteLine($"Could not start the eval host: {ex.Message}");
			Console.Error.WriteLine("The harness needs the same PostgreSQL test database as PerfumeTracker.xTests (ConnectionStrings:PerfumeTrackerTest, name must contain 'test').");
			return 2;
		}

		var fingerprint = await host.ComputePromptFingerprintAsync(cancellationToken);
		host.ResponsesClient.Fingerprint = fingerprint;
		Console.WriteLine($"Prompt fingerprint: {fingerprint}");
		Console.WriteLine($"Tools: {string.Join(", ", host.RegisteredToolNames)} | marketplace offers: {(host.MarketplaceAvailable ? "present" : "absent")}");

		var judgeModel = options.JudgeModel ?? options.Model;
		var judgeClient = BuildJudgeClient(options, host, judgeModel);
		if (judgeModel != null && judgeClient == null) {
			Console.WriteLine("Judge requested but no usable API key was found; rubric scoring will be skipped.");
		}

		var scoringContext = new EvalScoringContext {
			OwnedPerfumeNames = await host.GetOwnedPerfumeNamesAsync(cancellationToken),
			RegisteredToolNames = host.RegisteredToolNames,
			MaxIterations = host.MaxIterations,
			MaxResultsPerToolCall = host.MaxResultsPerToolCall,
			MarketplaceAvailable = host.MarketplaceAvailable,
			JudgeClient = judgeClient,
			JudgeModel = judgeClient == null ? null : judgeModel
		};
		var scorers = CreateScorers();
		var results = new List<CaseResult>();

		Console.WriteLine();
		foreach (var evalCase in cases) {
			if (cancellationToken.IsCancellationRequested) break;

			if (options.Mode == EvalMode.Replay && evalCase.RequiresApi) {
				Console.WriteLine($"  SKIP  {evalCase.Id} (requires API calls that replay cannot reproduce)");
				results.Add(new CaseResult {
					CaseId = evalCase.Id,
					Category = evalCase.Category,
					Status = CaseStatus.Skipped,
					Note = "requires API (embeddings/chat) and cannot be replayed"
				});
				continue;
			}

			var trajectory = await new ChatAgentRunner(host).RunAsync(evalCase, fingerprint, cancellationToken);
			var scores = new List<ScoreResult>();
			foreach (var scorer in scorers) {
				scores.Add(await scorer.ScoreAsync(evalCase, trajectory, scoringContext, cancellationToken));
			}

			var status = trajectory.Error != null
				? CaseStatus.Error
				: scores.Any(score => score.Outcome == ScoreOutcome.Fail) ? CaseStatus.Failed : CaseStatus.Passed;
			var note = trajectory.Error != null && options.Mode == EvalMode.Replay
				? $"{trajectory.Error} (record it with --mode record)"
				: trajectory.Error;

			results.Add(new CaseResult {
				CaseId = evalCase.Id,
				Category = evalCase.Category,
				Status = status,
				Scores = scores,
				Trajectory = trajectory,
				Note = note
			});

			var failedScorers = scores.Where(score => score.Outcome == ScoreOutcome.Fail).Select(score => score.Scorer).ToList();
			Console.WriteLine($"  {Status(status),-6} {evalCase.Id} [{trajectory.ChatModelCalls} calls, {trajectory.DurationMs} ms]"
				+ (failedScorers.Count > 0 ? $" failed: {string.Join(", ", failedScorers)}" : string.Empty));
			if (status == CaseStatus.Error) Console.WriteLine($"         {trajectory.Error}");
		}

		runStopwatch.Stop();

		var report = new EvalReport {
			Mode = options.Mode.ToString(),
			PromptFingerprint = fingerprint,
			Model = options.Mode == EvalMode.Replay ? host.ConfigurationValue("OpenAI:AssistantModel") : options.Model,
			JudgeModel = scoringContext.JudgeModel,
			StartedAt = startedAt,
			DurationMs = runStopwatch.ElapsedMilliseconds,
			Passed = results.Count(result => result.Status == CaseStatus.Passed),
			Failed = results.Count(result => result.Status == CaseStatus.Failed),
			Skipped = results.Count(result => result.Status == CaseStatus.Skipped),
			Errored = results.Count(result => result.Status == CaseStatus.Error),
			InputTokens = results.Where(r => r.Trajectory != null).Sum(r => r.Trajectory!.InputTokens),
			OutputTokens = results.Where(r => r.Trajectory != null).Sum(r => r.Trajectory!.OutputTokens),
			Cases = results
		};

		await ReportWriter.WriteAsync(report, outputDirectory, baselinePath, cancellationToken);
		var summaryPath = Path.Combine(outputDirectory, "summary.md");

		Console.WriteLine();
		Console.WriteLine($"Passed {report.Passed} | Failed {report.Failed} | Errored {report.Errored} | Skipped {report.Skipped}");
		Console.WriteLine($"Report: {summaryPath}");
		if (baselinePath != null) {
			var comparison = BaselineComparison.Compare(baselinePath, results);
			foreach (var line in comparison) Console.WriteLine(line);
			if (comparison.Any(line => line.Contains("REGRESSED", StringComparison.Ordinal))) {
				Console.Error.WriteLine("Regression detected versus baseline.");
				return 1;
			}
		}

		if (report.Passed == 0 && report.Failed == 0 && report.Errored == 0) {
			Console.Error.WriteLine("No case produced a scored run.");
			return 2;
		}

		if (options.FailOnAnyFailure && (report.Failed > 0 || report.Errored > 0)) {
			return 1;
		}
		return 0;
	}

	private static IResponsesClient? BuildJudgeClient(EvalRunOptions options, EvalHost host, string? judgeModel) {
		if (options.SkipJudge || string.IsNullOrWhiteSpace(judgeModel)) return null;
		var apiKey = options.ApiKey ?? host.ConfigurationValue("OpenAI:ApiKey");
		if (string.IsNullOrWhiteSpace(apiKey) || apiKey.StartsWith("eval-replay", StringComparison.Ordinal)) return null;
		return new ResponsesClientAdapter(new OpenAIClient(apiKey).GetResponsesClient());
	}

	private static List<IEvalScorer> CreateScorers() => [
		new CompletionScorer(),
		new ToolSelectionScorer(),
		new ToolArgumentScorer(),
		new PerfumeReferenceScorer(),
		new IterationBudgetScorer(),
		new TitleScorer(),
		new LlmJudgeScorer()
	];

	private static string Status(CaseStatus status) => status switch {
		CaseStatus.Passed => "PASS",
		CaseStatus.Failed => "FAIL",
		CaseStatus.Skipped => "SKIP",
		_ => "ERROR"
	};
}
