using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Lapak.Components;
using Lapak.Data;
using Lapak.Models;
using Lapak.Models.Configurations;
using Lapak.Services;
using Lapak.Services.AI;
using Lapak.Services.SemanticKernel;
using Lapak.Services.Rag;
using Lapak.Services.Storage;
using Lapak.Services.Payment;
using Lapak.Services.Shipping;
using Lapak.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// 🔹 DATABASE CONFIGURATION
// ============================================
var dbProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
var connectionString = dbProvider switch
{
    "SqlServer" => builder.Configuration.GetConnectionString("SqlServer"),
    "MySql" => builder.Configuration.GetConnectionString("MySql"),
    "PostgreSql" => builder.Configuration.GetConnectionString("PostgreSql"),
    _ => builder.Configuration.GetConnectionString("DefaultConnection")
};

builder.Services.AddDbContext<LapakDbContext>(options =>
{
    switch (dbProvider)
    {
        case "SqlServer":
            options.UseSqlServer(connectionString);
            break;
        case "MySql":
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            break;
        case "PostgreSql":
            options.UseNpgsql(connectionString);
            break;
        default:
            options.UseSqlite(connectionString);
            break;
    }
});

// ============================================
// 🔹 IDENTITY & AUTHENTICATION
// ============================================
builder.Services.AddIdentity<User, IdentityRole<Guid>>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false;
})
.AddEntityFrameworkStores<LapakDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.LogoutPath = "/account/logout";
    options.AccessDeniedPath = "/account/access-denied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
});

// ============================================
// 🔹 CONFIGURATION BINDINGS
// ============================================
builder.Services.Configure<AiConfig>(builder.Configuration.GetSection("AI"));
builder.Services.Configure<VectorDatabaseConfig>(builder.Configuration.GetSection("VectorDatabase"));
builder.Services.Configure<StorageConfig>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<PaymentGatewayConfig>(builder.Configuration.GetSection("PaymentGateways"));
builder.Services.Configure<ShippingConfig>(builder.Configuration.GetSection("Shipping"));
builder.Services.Configure<CustomerScoringConfig>(builder.Configuration.GetSection("CustomerScoring"));
builder.Services.Configure<RecommendationConfig>(builder.Configuration.GetSection("RecommendationEngine"));

// ============================================
// 🔹 HTTP CLIENTS
// ============================================
builder.Services.AddHttpClient("LlmClient", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("PaymentClient", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("ShippingClient", client =>
{
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// ============================================
// 🔹 SEMANTIC KERNEL AI SERVICE (Scoped - uses DbContext)
// ============================================
builder.Services.AddScoped<ISkChatService, SkChatService>();

// ============================================
// 🔹 VECTOR RAG SERVICE (Singleton - no scoped deps)
// ============================================
builder.Services.AddSingleton<IVectorRagService, VectorRagService>();
builder.Services.AddHostedService<VectorIndexingBackgroundService>();

// ============================================
// 🔹 STORAGE SERVICES
// ============================================
builder.Services.AddScoped<FileSystemStorageService>();
builder.Services.AddScoped<MinioStorageService>();
builder.Services.AddScoped<StorageServiceFactory>();

// ============================================
// 🔹 PAYMENT SERVICE
// ============================================
builder.Services.AddScoped<IPaymentService, PaymentService>();

// ============================================
// 🔹 SHIPPING SERVICE
// ============================================
builder.Services.AddScoped<IShippingService, ShippingService>();

// ============================================
// 🔹 BUSINESS SERVICES
// ============================================
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<ICustomerScoringService, CustomerScoringService>();

// ============================================
// 🔹 SIGNALR
// ============================================
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 128 * 1024;
});

// ============================================
// 🔹 BLAZOR & CONTROLLERS
// ============================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();

var app = builder.Build();

// ============================================
// 🔹 DATABASE INITIALIZATION
// ============================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LapakDbContext>();
    db.Database.EnsureCreated();

    if (!db.Categories.Any())
    {
        SeedData.Initialize(db);
    }
}

// ============================================
// 🔹 MIDDLEWARE
// ============================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<DashboardHub>("/hubs/dashboard");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
