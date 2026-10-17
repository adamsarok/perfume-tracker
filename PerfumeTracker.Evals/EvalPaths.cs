namespace PerfumeTracker.Evals;

/// <summary>
/// Resolves paths relative to the eval project so recordings and reports land next to the
/// source instead of inside bin/, which keeps them easy to inspect, diff and commit.
/// </summary>
public static class EvalPaths {
	public static string ProjectDirectory { get; } = FindProjectDirectory();

	public static string DefaultCaseFile => Path.Combine(ProjectDirectory, "Cases", "chat-agent.cases.json");
	public static string DefaultRecordingsDirectory => Path.Combine(ProjectDirectory, "Recordings");
	public static string DefaultResultsDirectory => Path.Combine(ProjectDirectory, "results");

	private static string FindProjectDirectory() {
		var directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory != null) {
			if (File.Exists(Path.Combine(directory.FullName, "PerfumeTracker.Evals.csproj"))) return directory.FullName;
			directory = directory.Parent;
		}
		return AppContext.BaseDirectory;
	}

	/// <summary>Relative paths are resolved against the eval project, not the current directory.</summary>
	public static string Resolve(string path) =>
		Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(ProjectDirectory, path));
}
