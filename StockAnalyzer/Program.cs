using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StockAnalyzer.Components;
using StockAnalyzer.Data;
using StockAnalyzer.Models;
using StockAnalyzer.Services.StockData;
using StockAnalyzer.Services.LLM;
using StockAnalyzer.Services.Storage;
using StockAnalyzer.Services.Recommendation;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// DATABASE CONFIGURATION
// ============================================
var dbProvider = builder.Configuration["Database:Provider"] ?? "SQLite";
var connectionString = builder.Configuration["Database:ConnectionString"] ?? "Data Source=Data/stockanalyzer.db";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    switch (dbProvider)
    {
        case "SqlServer":
            options.UseSqlServer(connectionString);
            break;
        case "SQLite":
        default:
            options.UseSqlite(connectionString);
            break;
    }
});

// ============================================
// ASP.NET CORE IDENTITY
// ============================================
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.LoginPath = "/login";
    options.LogoutPath = "/logout";
    options.AccessDeniedPath = "/login";
    options.SlidingExpiration = true;
});

// ============================================
// SERVICES REGISTRATION
// ============================================

// Stock Data Services
builder.Services.AddScoped<IStockDataService, StockDataService>();
builder.Services.AddScoped<ITechnicalAnalysisService, TechnicalAnalysisService>();
builder.Services.AddScoped<IFundamentalAnalysisService, FundamentalAnalysisService>();
builder.Services.AddScoped<ISentimentAnalysisService, SentimentAnalysisService>();
builder.Services.AddScoped<INewsScrapingService, NewsScrapingService>();

// LLM Provider Registrations
builder.Services.AddSingleton<OpenAIProvider>();
builder.Services.AddSingleton<GeminiProvider>();
builder.Services.AddSingleton<AnthropicProvider>();
builder.Services.AddSingleton<OllamaProvider>();
builder.Services.AddSingleton<OpenAICompatibleProvider>();

// LLM Services
builder.Services.AddSingleton<ILLMProviderFactory, LLMProviderFactory>();
builder.Services.AddScoped<ILLMService, LLMService>();
builder.Services.AddScoped<ILLMConfigService, LLMConfigService>();

// Storage Provider Registrations
builder.Services.AddSingleton<FileSystemStorageService>();
builder.Services.AddSingleton<S3StorageService>();
builder.Services.AddSingleton<AzureBlobStorageService>();

// Storage Services
builder.Services.AddSingleton<IStorageServiceFactory, StorageServiceFactory>();
builder.Services.AddScoped<IStorageService>(sp =>
{
    var factory = sp.GetRequiredService<IStorageServiceFactory>();
    return factory.CreateStorageService();
});

// Recommendation Services
builder.Services.AddScoped<IRecommendationService, RecommendationService>();

// HTTP Clients
builder.Services.AddHttpClient("StockApi", client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "StockAnalyzer/1.0");
});

builder.Services.AddHttpClient("LLMClient", client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});

// Add Blazor services with interactive server components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add auth state provider for Blazor
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// ============================================
// DATABASE INITIALIZATION & SEEDING
// ============================================
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // Ensure database is created with seed data
        dbContext.Database.EnsureCreated();

        // Seed users if not exist (Identity doesn't auto-seed via EnsureCreated for users)
        if (!dbContext.Users.Any(u => u.UserName == "admin@stockanalyzer.com"))
        {
            var adminUser = new ApplicationUser
            {
                UserName = "admin@stockanalyzer.com",
                Email = "admin@stockanalyzer.com",
                DisplayName = "Admin StockAnalyzer",
                PreferredTheme = "light",
                EmailConfirmed = true,
                RegisteredAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(adminUser, "Admin123!");
        }

        if (!dbContext.Users.Any(u => u.UserName == "demo@stockanalyzer.com"))
        {
            var demoUser = new ApplicationUser
            {
                UserName = "demo@stockanalyzer.com",
                Email = "demo@stockanalyzer.com",
                DisplayName = "Demo User",
                PreferredTheme = "dark",
                EmailConfirmed = true,
                RegisteredAt = DateTime.UtcNow
            };
            await userManager.CreateAsync(demoUser, "Demo123!");
        }

        // Sync LLM configs
        var configService = scope.ServiceProvider.GetRequiredService<ILLMConfigService>();
        await configService.SyncConfigFromAppSettingsAsync();
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error initializing database: {Message}", ex.Message);
    }
}

// ============================================
// MIDDLEWARE PIPELINE
// ============================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add logout endpoint
app.MapGet("/logout", async (HttpContext context, SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    context.Response.Redirect("/login");
});

app.Run();
