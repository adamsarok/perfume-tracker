using Carter;
using HealthChecks.UI.Client;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server;
using PerfumeTracker.Server.Repo;
using PerfumeTrackerAPI.Models;
using PerfumeTrackerAPI.Repo;
using PerfumeTrackerAPI.Server.Helpers;

var builder = WebApplication.CreateBuilder(args);

string? conn;
conn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(conn)) conn = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(conn)) throw new ConfigEmptyException("Connection string is empty");

builder.Services.AddDbContext<PerfumetrackerContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddScoped<PerfumeRepo>();
builder.Services.AddScoped<TagRepo>();
builder.Services.AddScoped<PerfumeWornRepo>();
builder.Services.AddScoped<PerfumeSuggestedRepo>();
builder.Services.AddScoped<RecommendationsRepo>();
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


var app = builder.Build();

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