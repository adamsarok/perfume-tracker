using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Behaviors;
using PerfumeTracker.Server.Config;
using PerfumeTracker.Server.Features.Achievements;
using PerfumeTracker.Server.Features.Common;
using PerfumeTracker.Server.Features.Demo;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Features.R2;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Helpers;
using PerfumeTracker.Server.Middleware;
using PerfumeTracker.Server.Server.Helpers;
using PerfumeTracker.Server.Startup;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using System;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading.RateLimiting;
using static PerfumeTracker.Server.Features.Missions.ProgressMissions;

var builder = WebApplication.CreateBuilder(args);

string? conn;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl)) {
	var uri = new Uri(databaseUrl);
	var username = uri.UserInfo.Split(':')[0];
	var password = uri.UserInfo.Split(':')[1];
	conn = $"Host={uri.Host};Database={uri.AbsolutePath.Substring(1)};Username={username};Password={password};Port={uri.Port};SSL Mode=Require"; 
} else {
	conn = builder.Configuration.GetConnectionString("PerfumeTrackerTest");
	if (string.IsNullOrWhiteSpace(conn)) throw new ConfigEmptyException("Connection string is empty");
}

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

var defaultLogLevel = builder.Configuration.GetValue<string>("Logging:LogLevel:Default") ?? "Information";
loggerConfig.MinimumLevel.ControlledBy(new LoggingLevelSwitch(
	(LogEventLevel)Enum.Parse(typeof(LogEventLevel), defaultLogLevel)));

Log.Logger = loggerConfig.CreateLogger();
builder.Host.UseSerilog();

builder.Services.AddDbContext<PerfumeTrackerContext>(opt => {
	opt.UseNpgsql(conn);
	opt.AddInterceptors(new EntityInterceptor());
});

Startup.SetupRateLimiting(builder.Services, builder.Configuration);
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
builder.Services.AddScoped<UploadImageHandler>();
builder.Services.AddCarter();
builder.Services.AddSignalR();

builder.Services.AddHealthChecks()
    .AddNpgSql(conn);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var corsConfig = new CorsConfiguration(builder.Configuration);
builder.Services.AddCors(options => {
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder
            .WithOrigins(corsConfig.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

builder.Services.AddSingleton<ISideEffectQueue, SideEffectQueue>();
builder.Services.AddHostedService<SideEffectProcessor>();

builder.Services.AddHostedService<OutboxService>();
builder.Services.AddHostedService<MissionService>();

builder.Services.AddHttpClient<UploadImageEndpoint>();

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
    var dbContext = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
	var seedUsers = scope.ServiceProvider.GetRequiredService<ISeedUsers>();
	var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
	await dbContext.Database.MigrateAsync();
	await SeedAchievements.SeedAchievementsAsync(dbContext);
	await SeedRoles.SeedRolesAsync(scope.ServiceProvider);
	await seedUsers.SeedAdminAsync();
	var demoUserId = await seedUsers.SeedDemoUserAsync();
	var uploadHandler = scope.ServiceProvider.GetRequiredService<UploadImageHandler>();
	var demoImages = new List<Guid>();
	try {
		demoImages = await SeedDemoImages.SeedDemoImagesAsync(uploadHandler);
	} catch (Exception ex) {
		Log.Error(ex, "Failed to seed demo images");
	}
	if (demoUserId != null) await SeedDemoData.SeedDemoDataAsync(dbContext, demoUserId.Value, demoImages);
}

app.UseRateLimiter();
app.UseExceptionHandler();
app.UseCors("AllowSpecificOrigin");
app.UseSecurityLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseSecurityHeaders();
app.MapCarter();
app.UseHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions {
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});
app.MapHub<MissionProgressHub>("/api/hubs/mission-progress");

app.Run();

public partial class Program { } //for integration tests