using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using VirtualDoctor.Components;
using VirtualDoctor.Data;
using VirtualDoctor.Models;
using VirtualDoctor.Services;
using VirtualDoctor.Services.AI;
using VirtualDoctor.Services.RAG;
using VirtualDoctor.Services.Storage;
using VirtualDoctor.Services.Export;
using VirtualDoctor.Hubs;
using VirtualDoctor.Workers;

var builder = WebApplication.CreateBuilder(args);

var appConfig = new AppConfig();
builder.Configuration.GetSection("Llm").Bind(appConfig.Llm);
builder.Configuration.GetSection("Database").Bind(appConfig.Database);
builder.Configuration.GetSection("VectorDb").Bind(appConfig.VectorDb);
builder.Configuration.GetSection("Storage").Bind(appConfig.Storage);
builder.Configuration.GetSection("Indexing").Bind(appConfig.Indexing);
builder.Configuration.GetSection("GoogleMaps").Bind(appConfig.GoogleMaps);
builder.Configuration.GetSection("Search").Bind(appConfig.Search);
builder.Configuration.GetSection("Meeting").Bind(appConfig.Meeting);
builder.Configuration.GetSection("Payment").Bind(appConfig.Payment);

builder.Services.AddSingleton(appConfig);
builder.Services.AddSingleton(appConfig.Llm);
builder.Services.AddSingleton(appConfig.Storage);
builder.Services.AddSingleton(appConfig.VectorDb);
builder.Services.AddSingleton(appConfig.Search);
builder.Services.AddSingleton(appConfig.GoogleMaps);
builder.Services.AddSingleton(appConfig.Meeting);
builder.Services.AddSingleton(appConfig.Payment);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    var cs = appConfig.Database.ConnectionString;
    switch (appConfig.Database.Provider)
    {
        case "SqlServer": options.UseSqlServer(cs); break;
        case "PostgreSql": options.UseNpgsql(cs); break;
        case "MySql": options.UseMySql(cs, ServerVersion.AutoDetect(cs)); break;
        default: options.UseSqlite(cs); break;
    }
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/auth/login";
        options.LogoutPath = "/auth/logout";
        // Harus berupa path saja. Query string di sini akan ter-escape dan menghasilkan URL rusak.
        options.AccessDeniedPath = "/akses-ditolak";
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.IsEssential = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.MaxAge = TimeSpan.FromDays(7);
    });

builder.Services.AddAuthorization(options => { options.FallbackPolicy = options.DefaultPolicy; });
builder.Services.AddHttpContextAccessor();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddSignalR();

builder.Services.AddHttpClient("LlmClient", c =>
{
    c.Timeout = TimeSpan.FromMinutes(5);
    c.DefaultRequestHeaders.Add("User-Agent", "VirtualDoctor/1.0");
});
builder.Services.AddHttpClient("MeetingClient", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.Add("User-Agent", "VirtualDoctor/1.0");
});
builder.Services.AddHttpClient("PaymentClient", c =>
{
    c.Timeout = TimeSpan.FromSeconds(30);
    c.DefaultRequestHeaders.Add("User-Agent", "VirtualDoctor/1.0");
});
builder.Services.AddEmbeddingGenerator<string, Embedding<float>>(sp => new SimpleEmbeddingGenerator());

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDoctorService, DoctorService>();
builder.Services.AddScoped<IMedicineService, MedicineService>();
builder.Services.AddScoped<IHospitalService, HospitalService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IDoctorScheduleService, DoctorScheduleService>();
builder.Services.AddScoped<IConsultationService, ConsultationService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IHomecareService, HomecareAppService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IInsuranceService, InsuranceService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddSingleton<ILlmProviderFactory, LlmProviderFactory>();
builder.Services.AddScoped<IAiChatService, AiChatService>();
builder.Services.AddScoped<IKernelFunctionService, KernelFunctionService>();
builder.Services.AddScoped<IVectorStoreService, VectorStoreService>();
builder.Services.AddScoped<IDocumentIndexingService, DocumentIndexingService>();
builder.Services.AddScoped<IRagQueryService, RagQueryService>();
builder.Services.AddSingleton<IFileStorageService>(sp => StorageServiceFactory.Create(sp));
builder.Services.AddSingleton<ILocationService, LocationService>();
builder.Services.AddSingleton<ISearchService, SearchService>();
builder.Services.AddSingleton<IExportService, ExportService>();
builder.Services.AddSingleton<VirtualDoctor.Services.Meeting.IMeetingService, VirtualDoctor.Services.Meeting.MeetingService>();
builder.Services.AddScoped<VirtualDoctor.Services.Analytics.IDashboardService, VirtualDoctor.Services.Analytics.DashboardService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<VirtualDoctor.Services.Payment.IPaymentService, VirtualDoctor.Services.Payment.PaymentService>();
builder.Services.AddScoped<VirtualDoctor.Services.Payment.IPaymentWebhookService, VirtualDoctor.Services.Payment.PaymentWebhookService>();

if (appConfig.Indexing.AutoIndex) builder.Services.AddHostedService<PdfIndexingWorker>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var storage = scope.ServiceProvider.GetRequiredService<IFileStorageService>();
    await db.Database.EnsureCreatedAsync();

    // EnsureCreated tidak menyentuh database lama, jadi selisih skema ditambal di sini.
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    await SchemaUpgrader.UpgradeAsync(db, appConfig.Database.Provider, loggerFactory.CreateLogger("Schema"));

    await DataSeeder.SeedAsync(db, storage);

    // Peran pengguna: melengkapi akun yang belum punya, termasuk memindahkan
    // konvensi admin-berdasarkan-email lama menjadi data.
    await DataSeeder.EnsureRolesAsync(db, loggerFactory.CreateLogger("Roles"));

    // Transaksi contoh hanya untuk Development, dan hanya bila database masih kosong.
    if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("Seed:DemoTransactions", false))
        await DemoDataSeeder.SeedAsync(db, loggerFactory.CreateLogger("DemoData"));

    // Override konfigurasi dari halaman Pengaturan menimpa appsettings.json
    await SettingsService.ApplyStoredOverridesAsync(db, appConfig, loggerFactory.CreateLogger("Settings"));

    // Debug: cek user ada
    var userCount = await db.Users.CountAsync();
    Console.WriteLine($"[STARTUP] Users in DB: {userCount}");
    var testUser = await db.Users.FirstOrDefaultAsync(u => u.Email == "budi@email.com");
    if (testUser != null)
    {
        var ph = await db.Set<PasswordHash>().FirstOrDefaultAsync(p => p.UserId == testUser.Id);
        Console.WriteLine($"[STARTUP] Test user: {testUser.Email} / Hash exists: {ph != null}");
    }
}

if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Error", true); app.UseHsts(); }
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseStaticFiles();

// Antiforgery SKIP untuk /auth/ paths
app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/auth/"), subApp =>
{
    subApp.UseAntiforgery();
});

app.UseAuthentication();
app.UseAuthorization();

// ============================================
// AUTH ENDPOINTS
// ============================================

app.MapPost("/auth/login-handler", async (HttpContext ctx, AppDbContext db) =>
{
    try
    {
        var form = await ctx.Request.ReadFormAsync();
        var email = AuthHelpers.NormalizeEmail(form["email"].FirstOrDefault());
        var password = form["password"].FirstOrDefault() ?? string.Empty;
        var remember = string.Equals(form["remember"].FirstOrDefault(), "true", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine($"[LOGIN] Attempt: {email}");

        if (!AuthHelpers.IsValidEmail(email) || string.IsNullOrWhiteSpace(password))
        {
            ctx.Response.Redirect("/auth/login?error=invalid");
            return;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        if (user == null)
        {
            Console.WriteLine($"[LOGIN] FAIL - User not found: {email}");
            ctx.Response.Redirect("/auth/login?error=invalid");
            return;
        }

        var storedHash = await db.Set<PasswordHash>().FirstOrDefaultAsync(p => p.UserId == user.Id);
        var check = AuthHelpers.VerifyPassword(storedHash?.Hash, password);

        if (check == PasswordCheck.Failed)
        {
            Console.WriteLine($"[LOGIN] FAIL - Bad password for: {email}");
            ctx.Response.Redirect("/auth/login?error=invalid");
            return;
        }

        // Hash format lama ditulis ulang dengan PBKDF2 setelah sandi terbukti benar.
        if (check == PasswordCheck.SuccessNeedsRehash && storedHash != null)
        {
            storedHash.Hash = AuthHelpers.HashPassword(password);
            await db.SaveChangesAsync();
            Console.WriteLine($"[LOGIN] Hash diperbarui ke format baru untuk {email}");
        }

        Console.WriteLine($"[LOGIN] OK - {email} ({user.FullName})");

        var principal = await AuthClaims.BuildAsync(db, user);
        var props = new AuthenticationProperties
        {
            IsPersistent = remember,
            AllowRefresh = true
        };

        if (remember)
        {
            props.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7);
        }

        await ctx.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            props);

        ctx.Response.Redirect("/");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[LOGIN] EXCEPTION: {ex}");
        ctx.Response.Redirect("/auth/login?error=invalid");
    }
}).AllowAnonymous();

app.MapPost("/auth/register-handler", async (HttpContext ctx, AppDbContext db) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var fullName = (form["fullName"].FirstOrDefault() ?? string.Empty).Trim();
    var email = AuthHelpers.NormalizeEmail(form["email"].FirstOrDefault());
    var password = form["password"].FirstOrDefault() ?? string.Empty;
    var confirm = form["confirmPassword"].FirstOrDefault() ?? string.Empty;

    if (string.IsNullOrWhiteSpace(fullName)) { ctx.Response.Redirect("/auth/register?error=name"); return; }
    if (!AuthHelpers.IsValidEmail(email)) { ctx.Response.Redirect("/auth/register?error=email"); return; }
    if (AuthHelpers.ValidatePasswordRules(password) != null || password != confirm) { ctx.Response.Redirect("/auth/register?error=password"); return; }
    if (await db.Users.AnyAsync(u => u.Email == email)) { ctx.Response.Redirect("/auth/register?error=email"); return; }

    var user = new ApplicationUser
    {
        Email = email,
        FullName = fullName,
        CreatedAt = DateTime.UtcNow,
        IsActive = true
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    var hash = AuthHelpers.HashPassword(password);
    db.Set<PasswordHash>().Add(new PasswordHash { UserId = user.Id, Hash = hash });
    db.UserRoles.Add(new UserRole { UserId = user.Id, Role = AppRoles.Patient });
    await db.SaveChangesAsync();

    ctx.Response.Redirect("/auth/register?registered=true");
}).AllowAnonymous();

app.MapPost("/auth/reset-password-handler", async (HttpContext ctx, AppDbContext db) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var email = AuthHelpers.NormalizeEmail(form["email"].FirstOrDefault());

    if (!AuthHelpers.IsValidEmail(email))
    {
        ctx.Response.Redirect("/auth/reset-password?error=notfound");
        return;
    }

    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
    if (user == null) { ctx.Response.Redirect("/auth/reset-password?error=notfound"); return; }

    // Catatan: alur reset ini masih menetapkan sandi sementara yang sama untuk semua akun.
    // Penggantian dengan tautan reset bertoken ada di roadmap.
    var newHash = AuthHelpers.HashPassword("Reset123!");
    var existing = await db.Set<PasswordHash>().FirstOrDefaultAsync(p => p.UserId == user.Id);
    if (existing != null) existing.Hash = newHash;
    else db.Set<PasswordHash>().Add(new PasswordHash { UserId = user.Id, Hash = newHash });
    await db.SaveChangesAsync();

    ctx.Response.Redirect("/auth/reset-password?sent=true");
}).AllowAnonymous();

app.MapGet("/auth/logout", async (HttpContext ctx) =>
{
    await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    ctx.Response.Redirect("/auth/login");
}).AllowAnonymous();

// ============================================
// WEBHOOK PENYEDIA PEMBAYARAN
// Dipanggil server penyedia, bukan browser, jadi tanpa cookie auth.
// Seluruh pemeriksaan keaslian, pencatatan jejak, dan penolakan kiriman ulang
// ada di PaymentWebhookService supaya perilakunya sama untuk kiriman yang
// diproses ulang lewat /admin/webhooks.
// ============================================

static async Task<IResult> ReceiveWebhookAsync(
    HttpContext ctx, string provider, VirtualDoctor.Services.Payment.IPaymentWebhookService webhooks)
{
    using var reader = new StreamReader(ctx.Request.Body);
    var body = await reader.ReadToEndAsync();
    var token = ctx.Request.Headers["x-callback-token"].FirstOrDefault();

    var result = await webhooks.ReceiveAsync(provider, body, token, ctx.RequestAborted);
    return Results.Json(new { message = result.Message, outcome = result.Outcome.ToString() },
        statusCode: result.StatusCode);
}

app.MapPost("/api/payments/webhook/midtrans", (HttpContext ctx, VirtualDoctor.Services.Payment.IPaymentWebhookService webhooks) =>
    ReceiveWebhookAsync(ctx, "Midtrans", webhooks)).AllowAnonymous();

app.MapPost("/api/payments/webhook/xendit", (HttpContext ctx, VirtualDoctor.Services.Payment.IPaymentWebhookService webhooks) =>
    ReceiveWebhookAsync(ctx, "Xendit", webhooks)).AllowAnonymous();

// ============================================
// ADMIN EXPORT ENDPOINTS
// ============================================
app.MapGet("/admin/export/{entity}", async (HttpContext ctx, string entity, string? format, AppDbContext db, IExportService exporter) =>
{
    format ??= "csv";
    var isExcel = format.Equals("xlsx", StringComparison.OrdinalIgnoreCase) || format.Equals("excel", StringComparison.OrdinalIgnoreCase);

    entity = entity.ToLowerInvariant();
    byte[] bytes;
    string fileName;
    string contentType;

    switch (entity)
    {
        case "users":
            var users = await db.Users.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(users, "Users") : exporter.ToCsv(users);
            fileName = isExcel ? "users.xlsx" : "users.csv";
            break;
        case "doctors":
            var doctors = await db.Doctors.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(doctors, "Doctors") : exporter.ToCsv(doctors);
            fileName = isExcel ? "doctors.xlsx" : "doctors.csv";
            break;
        case "hospitals":
            var hospitals = await db.Hospitals.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(hospitals, "Hospitals") : exporter.ToCsv(hospitals);
            fileName = isExcel ? "hospitals.xlsx" : "hospitals.csv";
            break;
        case "medicines":
            var medicines = await db.Medicines.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(medicines, "Medicines") : exporter.ToCsv(medicines);
            fileName = isExcel ? "medicines.xlsx" : "medicines.csv";
            break;
        case "healtharticles":
        case "articles":
            var articles = await db.HealthArticles.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(articles, "HealthArticles") : exporter.ToCsv(articles);
            fileName = isExcel ? "health-articles.xlsx" : "health-articles.csv";
            break;
        case "appointments":
            var appointments = await db.Appointments.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(appointments, "Appointments") : exporter.ToCsv(appointments);
            fileName = isExcel ? "appointments.xlsx" : "appointments.csv";
            break;
        case "doctorschedules":
        case "doctor-schedules":
            var schedules = await db.DoctorSchedules.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(schedules, "DoctorSchedules") : exporter.ToCsv(schedules);
            fileName = isExcel ? "doctor-schedules.xlsx" : "doctor-schedules.csv";
            break;
        case "homecareservices":
        case "homecare":
            var homecare = await db.HomecareServices.AsNoTracking().ToListAsync();
            bytes = isExcel ? exporter.ToExcel(homecare, "HomecareServices") : exporter.ToCsv(homecare);
            fileName = isExcel ? "homecare-services.xlsx" : "homecare-services.csv";
            break;
        default:
            return Results.NotFound("Entity not supported");
    }

    contentType = isExcel ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" : "text/csv";
    return Results.File(bytes, contentType, fileName);
}).RequireAuthorization(policy => policy.RequireRole(AppRoles.Admin));

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<ConsultationHub>("/hubs/consultation");

app.Run();

file class SimpleEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    public EmbeddingGeneratorMetadata Metadata { get; } = new("Simple", new Uri("https://localhost"));
    public async Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken ct = default)
    {
        var r = values.Select(v => { var b = System.Text.Encoding.UTF8.GetBytes(v); var vec = new float[256]; for (int i = 0; i < 256; i++) vec[i] = MathF.Tanh(b[(i * 7) % b.Length] * 0.01f + i * 0.001f); return new Embedding<float>(vec); }).ToList();
        return new GeneratedEmbeddings<Embedding<float>>(r);
    }
    public TService? GetService<TService>(object? key = null) where TService : class => this as TService;
    public object? GetService(Type serviceType, object? key = null) => serviceType.IsInstanceOfType(this) ? this : null;
    public void Dispose() { }
}
