using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Behaviors;
using PerfumeTracker.Server.Features.Achievements;
using PerfumeTracker.Server.Features.Auth;
using PerfumeTracker.Server.Features.Missions;
using PerfumeTracker.Server.Features.Outbox;
using PerfumeTracker.Server.Helpers;
using PerfumeTracker.Server.Server.Helpers;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using System;
using System.Diagnostics;
using System.Text;
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

builder.Services.AddIdentity<PerfumeIdentityUser, PerfumeIdentityRole>()
	.AddEntityFrameworkStores<PerfumeTrackerContext>()
	.AddDefaultTokenProviders();

builder.Services.Configure<IdentityOptions>(options => {
	options.Password.RequireDigit = false;
	options.Password.RequireLowercase = false;
	options.Password.RequireNonAlphanumeric = false;
	options.Password.RequireUppercase = false;
	options.Password.RequiredLength = 5;
	options.Password.RequiredUniqueChars = 1;

	options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
	options.Lockout.MaxFailedAccessAttempts = 5;
	options.Lockout.AllowedForNewUsers = true;

	options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
	options.User.RequireUniqueEmail = false;
});

builder.Services.ConfigureApplicationCookie(options => {
	options.Cookie.HttpOnly = true;
	options.ExpireTimeSpan = TimeSpan.FromHours(24);

	options.LoginPath = "/api/identity/account/login";
	options.AccessDeniedPath = "/api/identity/account/access-denied";
	options.SlidingExpiration = true;
});

builder.Services.AddAuthorization(options => {
	options.AddPolicy(Policies.READ, p => p.RequireRole(Roles.ADMIN, Roles.DEMO, Roles.USER));
	options.AddPolicy(Policies.WRITE, p => p.RequireRole(Roles.ADMIN, Roles.USER));
	options.AddPolicy(Policies.ADMIN, p => p.RequireRole(Roles.ADMIN));
});

builder.Services.AddAuthentication(options => {
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => {
	options.TokenValidationParameters = new TokenValidationParameters {
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		ValidAudience = builder.Configuration["Jwt:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(
			Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
	};
	options.Events = new JwtBearerEvents {
		OnMessageReceived = context => {
			context.Token = context.Request.Cookies["jwt"];
			return Task.CompletedTask;
		}
	};
});

var assembly = typeof(Program).Assembly;
builder.Services.AddMediatR(config => {
	config.RegisterServicesFromAssembly(assembly);
	config.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantProvider, TenantProvider>();
builder.Services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
builder.Services.AddScoped<UpsertUserProfile>();
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
	await SeedAchievements.SeedAchievementsAsync(dbContext);
	await SeedRoles.SeedRolesAsync(scope.ServiceProvider);
	await SeedAdmin.SeedAdminAsync(scope.ServiceProvider, dbContext);
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

app.UseAuthentication();
app.UseAuthorization();

app.Run();

public partial class Program { } //for integration tests