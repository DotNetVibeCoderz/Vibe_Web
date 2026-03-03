using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using SportTracker.Data;
using SportTracker.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=sporttracker.db";
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Add Scoped Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<DataService>();
builder.Services.AddSingleton<IStorageService, FileSystemStorageService>(); // Default
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Minimal API with Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Swagger for API
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SportTracker API V1");
});

app.UseRouting();

// Initialise Database
using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
}

// Map Minimal APIs
app.MapGet("/api/activities", async (IDbContextFactory<AppDbContext> factory) =>
{
    using var db = await factory.CreateDbContextAsync();
    var acts = await db.Activities.Select(a => new { a.Id, a.Title, a.Type, a.DistanceKm, a.Duration }).ToListAsync();
    return Results.Ok(acts);
});

app.MapGet("/api/leaderboard", async (IDbContextFactory<AppDbContext> factory) =>
{
    using var db = await factory.CreateDbContextAsync();
    var users = await db.Users.Include(u => u.Activities).ToListAsync();
    var top = users.Select(u => new { u.Username, TotalDistance = u.Activities.Sum(a => a.DistanceKm) })
                   .OrderByDescending(u => u.TotalDistance).Take(10).ToList();
    return Results.Ok(top);
});

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();