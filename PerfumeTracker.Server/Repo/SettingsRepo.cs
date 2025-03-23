using System;
using PerfumeTracker.Server.Models;
using PerfumeTrackerAPI.Models;

namespace PerfumeTracker.Server.Repo;

public class SettingsRepo(PerfumetrackerContext context) {
    public async Task<Settings?> GetSettings(string userId) {
        return await context.Settings.FindAsync(userId);
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
