using HealthChecks.UI.Client;
using Microsoft.AspNetCore.HttpOverrides;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Behaviors;
using PerfumeTracker.Server.Features.R2;
using PerfumeTracker.Server.Features.Users;
using PerfumeTracker.Server.Middleware;
using PerfumeTracker.Server.Services.Achievements;
using PerfumeTracker.Server.Services.Auth;
using PerfumeTracker.Server.Services.Common;
using PerfumeTracker.Server.Services.Demo;
using PerfumeTracker.Server.Services.Embedding;
using PerfumeTracker.Server.Services.Missions;
using PerfumeTracker.Server.Services.Outbox;
using PerfumeTracker.Server.Startup;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using static PerfumeTracker.Server.Services.Missions.ProgressMissions;
using static PerfumeTracker.Server.Services.Streaks.ProgressStreaks;

var builder = WebApplication.CreateBuilder(args);

string? conn;
var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrWhiteSpace(databaseUrl)) {
	var uri = new Uri(databaseUrl);
	var username = uri.UserInfo.Split(':')[0];
	var password = uri.UserInfo.Split(':')[1];
	conn = $"Host={uri.Host};Database={uri.AbsolutePath.Substring(1)};Username={username};Password={password};Port={uri.Port};SSL Mode=Require";
} else {
	string db = builder.Environment.IsEnvironment("Test") ? "PerfumeTrackerTest" : "PerfumeTracker";
	conn = builder.Configuration.GetConnectionString(db);
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
	opt.UseNpgsql(conn, o => o.UseVector());
	opt.AddInterceptors(new EntityInterceptor());
});

var rateLimitConfig = builder.Configuration.GetSection("RateLimits").Get<RateLimitConfiguration>();
if (rateLimitConfig != null) Startup.SetupRateLimiting(builder.Services, rateLimitConfig);
Startup.SetupAuthorizations(builder.Services, builder.Configuration);

var assembly = typeof(Program).Assembly;
builder.Services.AddMediatR(config => {
	var licenseKey = builder.Configuration.GetValue<string>("Mediatr:LicenseKey");
	if (!string.IsNullOrWhiteSpace(licenseKey)) config.LicenseKey = licenseKey;
	config.RegisterServicesFromAssembly(assembly);
	config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// Config
builder.Services.AddOptionsWithValidateOnStart<CorsConfiguration>()
	.Bind(builder.Configuration.GetSection("CORS"))
	.ValidateDataAnnotations();
builder.Services.AddOptionsWithValidateOnStart<R2Configuration>()
	.Bind(builder.Configuration.GetSection("R2"))
	.ValidateDataAnnotations();
builder.Services.AddOptionsWithValidateOnStart<JwtConfiguration>()
	.Bind(builder.Configuration.GetSection("Jwt"))
	.ValidateDataAnnotations();
builder.Services.AddOptionsWithValidateOnStart<RateLimitConfiguration>()
	.Bind(builder.Configuration.GetSection("RateLimits"))
	.ValidateDataAnnotations();
builder.Services.AddOptionsWithValidateOnStart<UserConfiguration>()
	.Bind(builder.Configuration.GetSection("Users"))
	.ValidateDataAnnotations();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<UpdateMissionProgressHandler>();
builder.Services.AddScoped<UpdateStreakProgressHandler>();
builder.Services.AddScoped<ICreateUser, CreateUser>();
builder.Services.AddScoped<ISeedUsers, SeedUsers>();
builder.Services.AddScoped<IPresignedUrlService, PresignedUrlService>();
builder.Services.AddScoped<UploadImageHandler>();
builder.Services.AddScoped<XPService>();
builder.Services.AddCarter();
builder.Services.AddSignalR();

builder.Services.AddHealthChecks()
	.AddNpgSql(conn);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options => {
	var corsConfig = builder.Configuration.GetSection("CORS").Get<CorsConfiguration>();
	if (corsConfig?.AllowedOrigins != null && corsConfig.AllowedOrigins.Length > 0) {
		options.AddPolicy("AllowSpecificOrigin",
			policy => policy
				.WithOrigins(corsConfig.AllowedOrigins)
				.AllowAnyHeader()
				.AllowAnyMethod()
				.AllowCredentials());
	}
});

builder.Services.AddSingleton<ISideEffectQueue, SideEffectQueue>();
builder.Services.AddHostedService<SideEffectProcessor>();

builder.Services.AddHostedService<OutboxRetryService>();
builder.Services.AddHostedService<EmbeddingRetryService>();
builder.Services.AddHostedService<MissionService>();

builder.Services.AddHttpClient<UploadImageEndpoint>();

builder.Services.Configure<ForwardedHeadersOptions>(options => {
	options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
							  ForwardedHeaders.XForwardedProto |
							  ForwardedHeaders.XForwardedHost;
	options.KnownNetworks.Clear();
	options.KnownProxies.Clear();
});

var app = builder.Build();

using (var scope = app.Services.CreateScope()) {
	var dbContext = scope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
	var seedUsers = scope.ServiceProvider.GetRequiredService<ISeedUsers>();
	await dbContext.Database.MigrateAsync();
	await SeedAchievements.SeedAchievementsAsync(dbContext);
	await SeedRoles.SeedRolesAsync(scope.ServiceProvider);
	await seedUsers.SeedAdminAsync();
	var demoUserId = await seedUsers.SeedDemoUserAsync();
	await SeedDemoData.SeedDemoDataAsync(dbContext, demoUserId.Value);
}

if (!Env.IsDevelopment) app.UseHttpsRedirection();
app.UseForwardedHeaders();
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

await app.RunAsync();

#pragma warning disable S1118 // Utility classes should not have public constructors
public partial class Program { } //for integration tests
#pragma warning restore S1118 // Utility classes should not have public constructors