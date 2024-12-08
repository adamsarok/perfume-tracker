using Carter;
using Microsoft.EntityFrameworkCore;
using PerfumeTracker.Server.Repo;
using PerfumeTrackerAPI.Migrations;
using PerfumeTrackerAPI.Models;
using PerfumeTrackerAPI.Repo;
using PerfumeTrackerAPI.Server.Helpers;

var builder = WebApplication.CreateBuilder(args);

string? conn;
if (builder.Environment.IsDevelopment()) conn = builder.Configuration.GetConnectionString("DefaultConnection");
else {
    conn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(conn)) conn = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrWhiteSpace(conn)) throw new Exception("Connection string is empty");

builder.Services.AddDbContext<PerfumetrackerContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddScoped<PerfumeRepo>();
builder.Services.AddScoped<TagRepo>();
builder.Services.AddScoped<PerfumeWornRepo>();
builder.Services.AddScoped<PerfumeSuggestedRepo>();
builder.Services.AddCarter();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddCors(options => {
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.AllowAnyOrigin() //TODO!!! set up CORS
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});


var app = builder.Build();

app.MapCarter();
//app.UseCors("AllowSpecificOrigin");
app.UseCors(x => x.AllowAnyHeader()
    .AllowAnyMethod()
    .AllowCredentials()
    .WithOrigins("http://localhost", "http://192.168.1.79:3000", "https://192.168.1.79:3000", "https://localhost:3000", "http://localhost:3000"));

// Configure the HTTP request pipeline.

//app.UseHttpsRedirection();


app.Run();
