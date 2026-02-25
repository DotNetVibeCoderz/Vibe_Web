using FMSNet.Data;
using FMSNet.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=fmsnet.db";
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<AppUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddSingleton<ISimulationService, SimulationService>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// API Key Middleware (Simple)
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api"))
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("API Key missing");
            return;
        }
        
        var db = context.RequestServices.GetRequiredService<ApplicationDbContext>();
        var key = extractedApiKey.ToString();
        var valid = await db.ApiKeys.AnyAsync(k => k.Key == key && k.IsActive);
        if (!valid)
        {
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Invalid API Key");
            return;
        }
    }
    await next();
});

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Minimal API Endpoints
app.MapGet("/api/vehicles", async (IVehicleService service) =>
{
    return Results.Ok(await service.GetVehiclesAsync());
})
.WithName("GetVehicles");

app.MapPost("/api/vehicles/location", async (IVehicleService service, [FromBody] VehicleLocationUpdateModel model) =>
{
    var vehicle = await service.GetVehicleByIdAsync(model.Id);
    if (vehicle == null) return Results.NotFound();
    
    vehicle.Latitude = model.Latitude;
    vehicle.Longitude = model.Longitude;
    vehicle.LastUpdate = DateTime.UtcNow;
    
    await service.UpdateVehicleAsync(vehicle);
    return Results.Ok(vehicle);
})
.WithName("UpdateLocation");

// Ensure Database is Created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.EnsureCreated();
    
    // Start Simulation
    var sim = app.Services.GetRequiredService<ISimulationService>();
    //sim.Start(); // Simulation started automatically
}

app.Run();

public class VehicleLocationUpdateModel
{
    public int Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
