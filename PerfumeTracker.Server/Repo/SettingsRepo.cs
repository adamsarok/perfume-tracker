namespace PerfumeTracker.Server.Repo;

public class SettingsRepo(PerfumetrackerContext context) {
	private readonly Settings defaultSettings = new Settings() {
		MinimumRating = 8,
		DayFilter = 30,
		ShowFemalePerfumes = true,
		ShowMalePerfumes = true,
		ShowUnisexPerfumes = true,
		SprayAmount = 0.2M
	};
	private async Task<Settings?> GetSettings(string userId) {
        return await context.Settings.FindAsync(userId);
    }
	public async Task<Settings> GetSettingsOrDefault(string userId) {
		var result = await context.Settings.FindAsync(userId);
		if (result != null) return result;
		return defaultSettings;
	}
	public async Task<Settings> UpsertSettings(Settings settings) {
        var found = await GetSettings(settings.UserId);
        settings.Updated_At = DateTime.UtcNow;
        if (found != null) {
            context.Entry(found).CurrentValues.SetValues(settings);
        } else {
            settings.Created_At = DateTime.UtcNow;
            await context.Settings.AddAsync(settings);
        }
        await context.SaveChangesAsync();
        return settings;
    }
}
