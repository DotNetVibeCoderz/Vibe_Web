using AISTracker.Components;
using AISTracker.Data;
using AISTracker.Models;
using AISTracker.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorPages();

// Radzen Services
builder.Services.AddScoped<DialogService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TooltipService>();
builder.Services.AddScoped<ContextMenuService>();

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=aistracker.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Account/Login";
});

// Services
builder.Services.AddSingleton<IFileStorageService, FileSystemStorageService>();

// Register tracking service for DI + hosted service
builder.Services.AddSingleton<AISTrackingService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AISTrackingService>());

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseSwagger();
app.UseSwaggerUI();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapRazorPages();

// Minimal API Endpoints for AIS Data
app.MapGet("/api/vessels", async (ApplicationDbContext db) =>
{
    return await db.Vessels.ToListAsync();
})
.WithName("GetVessels");

app.MapGet("/api/vessels/{mmsi}", async (string mmsi, ApplicationDbContext db) =>
{
    var vessel = await db.Vessels.FirstOrDefaultAsync(v => v.MMSI == mmsi);
    return vessel is not null ? Results.Ok(vessel) : Results.NotFound();
})
.WithName("GetVesselByMMSI");

app.MapPost("/api/position", async (PositionReport report, ApplicationDbContext db) =>
{
    db.PositionReports.Add(report);
    await db.SaveChangesAsync();
    return Results.Created($"/api/position/{report.Id}", report);
})
.WithName("PostPositionReport");

// Initialize Database Seeding
using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
