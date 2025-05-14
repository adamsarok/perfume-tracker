using Carter;
using HealthChecks.UI.Client;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Features.Achievements;
using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Features.UserProfiles;
using PerfumeTracker.Server.Helpers;
using PerfumeTracker.Server.Repo;
using PerfumeTracker.Server.Server.Helpers;

var builder = WebApplication.CreateBuilder(args);

string? conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn)) throw new ConfigEmptyException("Connection string is empty");
builder.Services.AddDbContext<PerfumeTrackerContext>(opt => {
	opt.UseNpgsql(conn);
	opt.AddInterceptors(new EntityInterceptor());
});

var assembly = typeof(Program).Assembly;
builder.Services.AddMediatR(config => {
	config.RegisterServicesFromAssembly(assembly);
});

builder.Services.AddScoped<PerfumeRepo>();
builder.Services.AddScoped<TagRepo>();
builder.Services.AddScoped<PerfumeEventsRepo>();
builder.Services.AddScoped<PerfumeSuggestedRepo>();
builder.Services.AddScoped<RecommendationsRepo>();
builder.Services.AddScoped<PerfumePlaylistRepo>();
builder.Services.AddScoped<GetUserProfile>();
builder.Services.AddScoped<UpsertUserProfile>();
builder.Services.AddCarter();

builder.Services.AddHealthChecks()
    .AddNpgSql(conn);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.AllowAnyOrigin() //TODO!!! set up CORS
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});

builder.Services.AddHostedService<OutboxProcessor>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
    await dbContext.Database.MigrateAsync();
	await SeedAchievements.DoSeed(dbContext);
    await SeedUserProfiles.DoSeed(dbContext);
}

app.UseExceptionHandler();
app.MapCarter();
app.UseCors(x => x.AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins("http://localhost", "http://192.168.1.79:3000", "https://192.168.1.79:3000", "https://localhost:3000", "http://localhost:3000"));
app.UseHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();

public partial class Program { } //for integration tests