namespace PerfumeTracker.Server;

public static class Env {
	public static bool IsDevelopment => Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
}
