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

// CreateBuilder wires up the static web asset manifest only in Development, so a
// non-published `dotnet run` in any other environment cannot resolve framework
// assets such as blazor.web.js. Calling it explicitly is a no-op once published.
builder.WebHost.UseStaticWebAssets();

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
.AddClaimsPrincipalFactory<LapakClaimsPrincipalFactory>()
.AddDefaultTokenProviders();

// Page-level guards. UserType is emitted as a role claim by the factory above.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("SellerOnly", policy => policy.RequireRole("Seller", "Admin"));
});

builder.Services.AddCascadingAuthenticationState();

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
// Providers are resolved as a collection: adding a gateway means adding one
// IPaymentProvider registration here, nothing else.
// ============================================
builder.Services.AddScoped<IPaymentProvider, MidtransPaymentProvider>();
builder.Services.AddScoped<IPaymentProvider, XenditPaymentProvider>();
builder.Services.AddScoped<IPaymentProvider, StripePaymentProvider>();
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

// Files written at runtime (avatar and chat uploads under wwwroot/uploads) are not
// in the build-time asset manifest, so they still need the classic middleware.
app.UseStaticFiles();

app.UseAntiforgery();
app.UseAuthentication();
app.UseAuthorization();

// Serves the fingerprinted assets that @Assets[...] and <ImportMap> resolve to,
// including _framework/blazor.web.js. Without this the app has no interactivity
// outside Development — every button silently does nothing.
app.MapStaticAssets();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<DashboardHub>("/hubs/dashboard");

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
