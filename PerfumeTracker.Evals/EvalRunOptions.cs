namespace PerfumeTracker.Evals;

/// <summary>
/// <see cref="Real"/> calls the model and throws the trajectory away.
/// <see cref="Record"/> calls the model and writes every response under <c>Recordings/</c>.
/// <see cref="Replay"/> never calls the model: responses are read back from <c>Recordings/</c>.
/// </summary>
public enum EvalMode {
	Real,
	Record,
	Replay
}

public sealed record EvalRunOptions {
	public EvalMode Mode { get; init; } = EvalMode.Replay;
	public string? Filter { get; init; }
	public string? CaseFile { get; init; }
	public string? RecordingsDirectory { get; init; }
	public string? OutputDirectory { get; init; }
	public string? BaselinePath { get; init; }
	public string? ApiKey { get; init; }
	public string? Model { get; init; }
	public string? JudgeModel { get; init; }
	public bool SkipJudge { get; init; }
	public bool FailOnAnyFailure { get; init; } = true;

	public static string Usage => """
		PerfumeTracker chat agent eval harness.

		Usage:
		  dotnet run --project PerfumeTracker.Evals -- [options]

		Options:
		  --mode real|record|replay   real = call the model, record = call + save responses,
		                              replay = read responses from disk (default: replay).
		  --filter <text>             Only run cases whose id or category contains <text>.
		  --cases <path>              Dataset file (default: Cases/chat-agent.cases.json).
		  --recordings <dir>          Recorded response directory (default: Recordings/).
		  --out <dir>                 Report directory (default: results/).
		  --baseline <results.json>   Compare against a previous results.json and report regressions.
		  --api-key <key>             OpenAI key for real/record/judge calls (falls back to config).
		  --model <name>              Assistant model override (real/record/judge subject run).
		  --judge-model <name>        Model used by the rubric judge (default: --model).
		  --no-judge                  Skip rubric (LLM judge) scoring.
		  --no-fail                   Always exit 0, even when cases fail.
		  --help                      Show this text.

		Requires a PostgreSQL database containing "test" in its name, configured through
		ConnectionStrings:PerfumeTrackerTest (same requirement as PerfumeTracker.xTests).
		""";

	public static EvalRunOptions Parse(string[] args) {
		var options = new EvalRunOptions();
		for (var i = 0; i < args.Length; i++) {
			var arg = args[i];
			string Next(string name) {
				if (i + 1 >= args.Length) throw new ArgumentException($"{name} requires a value");
				return args[++i];
			}
			options = arg switch {
				"--mode" => options with { Mode = ParseMode(Next(arg)) },
				"--filter" => options with { Filter = Next(arg) },
				"--cases" => options with { CaseFile = Next(arg) },
				"--recordings" => options with { RecordingsDirectory = Next(arg) },
				"--out" => options with { OutputDirectory = Next(arg) },
				"--baseline" => options with { BaselinePath = Next(arg) },
				"--api-key" => options with { ApiKey = Next(arg) },
				"--model" => options with { Model = Next(arg) },
				"--judge-model" => options with { JudgeModel = Next(arg) },
				"--no-judge" => options with { SkipJudge = true },
				"--no-fail" => options with { FailOnAnyFailure = false },
				_ => throw new ArgumentException($"Unknown argument '{arg}'")
			};
		}
		return options;
	}

	private static EvalMode ParseMode(string value) => value.ToLowerInvariant() switch {
		"real" => EvalMode.Real,
		"record" => EvalMode.Record,
		"replay" => EvalMode.Replay,
		_ => throw new ArgumentException($"Unknown mode '{value}'. Expected real, record or replay.")
	};
}
