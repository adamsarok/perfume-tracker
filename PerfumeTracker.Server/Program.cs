using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Behaviors;
using PerfumeTracker.Server.Features.Achievements;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Features.R2;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Helpers;
using PerfumeTracker.Server.Server.Helpers;
using PerfumeTracker.Server.Startup;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using System;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.RateLimiting;
using static PerfumeTracker.Server.Features.Missions.ProgressMissions;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
string? conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn)) throw new ConfigEmptyException("Connection string is empty");

var loggerConfig = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.PostgreSQL(
        connectionString: conn,
        tableName: "log",
        columnOptions: new Dictionary<string, ColumnWriterBase>
        {
            { "message", new RenderedMessageColumnWriter() },
            { "message_template", new MessageTemplateColumnWriter() },
            { "level", new LevelColumnWriter() },
            { "timestamp", new TimestampColumnWriter() },
            { "exception", new ExceptionColumnWriter() },
            { "properties", new LogEventSerializedColumnWriter() }
        },
        needAutoCreateTable: true)
    .Enrich.FromLogContext();
    
if (!builder.Environment.IsDevelopment()) loggerConfig.MinimumLevel.Error();
Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<PerfumeTrackerContext>(opt => {
	opt.UseNpgsql(conn);
	opt.AddInterceptors(new EntityInterceptor());
});

Startup.SetupRateLimiting(builder.Services);
Startup.SetupAuthorizations(builder.Services, builder.Configuration);

var assembly = typeof(Program).Assembly;
builder.Services.AddMediatR(config => {
	config.RegisterServicesFromAssembly(assembly);
	config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<UpdateMissionProgressHandler>();
builder.Services.AddScoped<ICreateUser, CreateUser>();
builder.Services.AddScoped<ISeedUsers, SeedUsers>();
builder.Services.AddScoped<IPresignedUrlService, PresignedUrlService>();
builder.Services.AddScoped<R2Configuration>();
builder.Services.AddCarter();
builder.Services.AddSignalR();

builder.Services.AddHealthChecks()
    .AddNpgSql(conn);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins("http://localhost:3000", "https://localhost:3000", "http://192.168.1.79:3000", "https://192.168.1.79:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddHostedService<OutboxService>();
builder.Services.AddHostedService<MissionService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
	var seedUsers = scope.ServiceProvider.GetRequiredService<ISeedUsers>();
	await dbContext.Database.MigrateAsync();
	await SeedAchievements.SeedAchievementsAsync(dbContext);
	await SeedRoles.SeedRolesAsync(scope.ServiceProvider);
	await seedUsers.SeedAdminAsync();
	await seedUsers.SeedDemoUserAsync();
}

app.UseRateLimiter();
app.UseExceptionHandler();
app.MapCarter();
app.UseCors("AllowSpecificOrigin");
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHub<MissionProgressHub>("/api/hubs/mission-progress");

app.Run();

public partial class Program { } //for integration tests