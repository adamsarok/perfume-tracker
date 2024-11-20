using Microsoft.EntityFrameworkCore;
using PerfumeTrackerAPI.Models;

var builder = WebApplication.CreateBuilder(args);

string? conn;
if (builder.Environment.IsDevelopment()) conn = builder.Configuration.GetConnectionString("DefaultConnection");
else {
    conn = Environment.GetEnvironmentVariable("CONNECTION_STRING");
    if (string.IsNullOrWhiteSpace(conn)) conn = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrWhiteSpace(conn)) throw new Exception("Connection string is empty"); //bad ide to throw here?

// Add services to the container.
builder.Services.AddDbContext<PerfumetrackerContext>(opt => opt.UseNpgsql(conn));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapGet("/perfumes", async (PerfumetrackerContext db) =>
    await db.Perfumes.ToListAsync());

app.Run();
