using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.Migrations;
using PerfumeTrackerAPI.Models;
using PerfumeTrackerAPI.Repo;

var builder = WebApplication.CreateBuilder(args);

string? conn;
if (builder.Environment.IsDevelopment()) conn = builder.Configuration.GetConnectionString("DefaultConnection");
else {
    conn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(conn)) conn = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrWhiteSpace(conn)) throw new Exception("Connection string is empty");

// Add services to the container.
builder.Services.AddDbContext<PerfumetrackerContext>(opt => opt.UseNpgsql(conn));
builder.Services.AddTransient<PerfumeRepo>();
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddCors(options => {
    options.AddPolicy("AllowSpecificOrigin",
        builder => builder.AllowAnyOrigin() //TODO!!! set up CORS
                          .AllowAnyHeader()
                          .AllowAnyMethod());
});


var app = builder.Build();

app.UseCors("AllowSpecificOrigin");

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/perfumes", async (PerfumeRepo repo) =>
    await repo.GetPerfumesWithWorn(""));

app.MapGet("/perfumes/fulltext/{fulltext}", async (string fulltext, PerfumeRepo repo) =>  
    await repo.GetPerfumesWithWorn(fulltext));

app.Run();
