using MediaMonitoring.Components;
using MediaMonitoring.Data;
using MediaMonitoring.Services;
using MediaMonitoring.Services.Integrations;
using MediaMonitoring.Services.Reports;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- KONFIGURASI DATABASE ---
var connectionString = "Data Source=media_monitoring.db";
builder.Services.AddDbContext<MediaMonitoringContext>(options =>
    options.UseSqlite(connectionString));

// --- REGISTRASI ALL SERVICES ---
// Core Services
builder.Services.AddScoped<ConfigService>();
builder.Services.AddScoped<OsintEngineService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<MlNetSentimentService>();
builder.Services.AddScoped<AiTrendPredictionService>();

// Integration Services
builder.Services.AddScoped<DarkWebMonitorService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TwitterApiService>();
builder.Services.AddScoped<FacebookApiService>();
builder.Services.AddScoped<InstagramApiService>();
builder.Services.AddScoped<YouTubeApiService>();

// Report Services
builder.Services.AddScoped<ReportGenerationService>();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// --- INISIALISASI DATABASE & SEED DATA ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<MediaMonitoringContext>();
    await context.Database.EnsureCreatedAsync();

    // Inisialisasi konfigurasi default
    var configService = scope.ServiceProvider.GetRequiredService<ConfigService>();
    await configService.InitializeDefaultsAsync();

    // Initialize default admin user
    var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
    await authService.InitializeDefaultAdminAsync();

    Console.WriteLine("===========================================");
    Console.WriteLine("MEDIA MONITORING OSINT - ENHANCED EDITION");
    Console.WriteLine("===========================================");
    Console.WriteLine("Features Enabled:");
    Console.WriteLine("✓ ML.NET Sentiment Analysis");
    Console.WriteLine("✓ Dark Web Monitoring (Simulation)");
    Console.WriteLine("✓ PDF/Excel Report Generation");
    Console.WriteLine("✓ Email & Slack Notifications");
   Console.WriteLine("✓ Geospatial Analysis");
    Console.WriteLine("✓ Network Graph Visualization");
    Console.WriteLine("✓ AI Trend Prediction");
    Console.WriteLine("✓ Multi-Tenant Architecture Ready");
    Console.WriteLine("✓ User Authentication System");
    Console.WriteLine("===========================================");
    Console.WriteLine($"Dashboard: http://localhost:5111");
    Console.WriteLine($"Geo Map: http://localhost:5111/geomap");
    Console.WriteLine($"Network: http://localhost:5111/network");
    Console.WriteLine("Default Admin: admin / admin123");
    Console.WriteLine("===========================================");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();