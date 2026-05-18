using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleBidding.Data;
using SimpleBidding.Models;
using SimpleBidding.Services;

var builder = WebApplication.CreateBuilder(args);

// --- DATABASE CONFIGURATION ---
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString(dbProvider) 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContextFactory<ApplicationDbContext>(options => {
    switch (dbProvider.ToLower())
    {
        case "sqlserver":
            options.UseSqlServer(connectionString);
            break;
        case "mysql":
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            break;
        case "sqlite":
        default:
            options.UseSqlite(connectionString);
            break;
    }
});

builder.Services.AddScoped(p => 
    p.GetRequiredService<IDbContextFactory<ApplicationDbContext>>().CreateDbContext());

// --- STORAGE CONFIGURATION ---
var storageProvider = builder.Configuration["StorageProvider"] ?? "FileSystem";
switch (storageProvider.ToLower())
{
    case "azure":
        builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
        break;
    case "s3":
        builder.Services.AddScoped<IStorageService, S3StorageService>();
        break;
    case "filesystem":
    default:
        builder.Services.AddScoped<IStorageService, FileStorageService>();
        break;
}

// --- IDENTITY ---
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// --- BUSINESS SERVICES ---
builder.Services.AddScoped<AuctionService>();
builder.Services.AddScoped<BidService>();
builder.Services.AddScoped<AuditService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddSingleton<CurrencyService>(); // Add this
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<ApplicationUser>>();

var app = builder.Build();

var uploadPath = Path.Combine(app.Environment.WebRootPath ?? "wwwroot", "uploads");
if (!Directory.Exists(uploadPath)) Directory.CreateDirectory(uploadPath);

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Database Initialization Error");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
