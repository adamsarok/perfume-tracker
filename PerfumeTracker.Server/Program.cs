using HealthChecks.UI.Client;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Behaviors;
using PerfumeTracker.Server.Features.Achievements;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Helpers;
using PerfumeTracker.Server.Server.Helpers;
using static PerfumeTracker.Server.Features.Missions.ProgressMissions;

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
	config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddScoped<TagRepo>();
builder.Services.AddScoped<GetUserProfile>();
builder.Services.AddScoped<UpsertUserProfile>();
builder.Services.AddScoped<MissionService>();
builder.Services.AddScoped<UpdateMissionProgressHandler>();
builder.Services.AddCarter();
builder.Services.AddSignalR();

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

builder.Services.AddHostedService<OutboxService>();
builder.Services.AddHostedService<MissionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
    await dbContext.Database.MigrateAsync();
    await SeedUserProfiles.DoSeed(dbContext);
	await SeedAchievements.DoSeed(dbContext);
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
app.MapHub<MissionProgressHub>("/api/hubs/mission-progress");

app.Run();

public partial class Program { } //for integration tests