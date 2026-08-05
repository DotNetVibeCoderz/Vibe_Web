using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SMSNet.Components;
using SMSNet.Data;
using SMSNet.Models;
using SMSNet.Services;
using SMSNet.Services.Assistant;
using SMSNet.Services.Attendance;
using SMSNet.Services.Payments;
using SMSNet.Services.Scheduling;

var builder = WebApplication.CreateBuilder(args);

// The whole interface is Indonesian, so dates and numbers must be too. Without this
// every "dddd, dd MMMM yyyy" renders as "Wednesday, 05 August 2026" on a server whose
// OS locale is English — which is most of them.
var indonesian = new System.Globalization.CultureInfo("id-ID");
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = indonesian;
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = indonesian;

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();

// Registers the factory *and* a scoped ApplicationDbContext from one call.
// Pages that hold a Blazor circuit use the factory to get a short-lived context
// per operation; a single scoped context shared across awaits is what produces
// "A second operation was started on this context".
builder.Services.AddDbContextFactory<ApplicationDbContext>(
    options => options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")),
    lifetime: ServiceLifetime.Scoped);

builder.Services
    .AddIdentityCore<AppUser>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services
    .AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/auth/login";
    options.AccessDeniedPath = "/auth/access-denied";
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(AppRoles.Admin));
});

builder.Services.Configure<FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));
builder.Services.AddSingleton<IFileStorageFactory, FileStorageFactory>();

builder.Services.AddScoped<ToastService>();
builder.Services.AddScoped<AuthenticationStateProviderAccessor>();
builder.Services.AddScoped<AuditService>();

// --- Uploads, comments, and rich text ---------------------------------------
builder.Services.Configure<UploadSettings>(
    builder.Configuration.GetSection(UploadSettings.SectionName));
builder.Services.AddScoped<UploadService>();
builder.Services.AddScoped<CommentService>();
builder.Services.AddSingleton<HtmlContentSanitizer>();

// --- Timetable generation ---------------------------------------------------
builder.Services.AddScoped<TimetableGenerator>();
builder.Services.AddScoped<TimetableValidator>();

// --- QR attendance ----------------------------------------------------------
builder.Services.AddScoped<QrCodeService>();
builder.Services.AddScoped<CardTemplateService>();
builder.Services.AddScoped<QrAttendanceService>();

// --- Payments ---------------------------------------------------------------
builder.Services.Configure<PaymentOptions>(
    builder.Configuration.GetSection(PaymentOptions.SectionName));
builder.Services.AddHttpClient("payments", client => client.Timeout = TimeSpan.FromSeconds(30));
builder.Services.AddScoped<PaymentGatewayRegistry>();
builder.Services.AddScoped<PaymentService>();

// --- Assistant ("Pak Dedi") -------------------------------------------------
builder.Services.Configure<AssistantOptions>(
    builder.Configuration.GetSection(AssistantOptions.SectionName));

// Named client with a generous timeout: a tool-calling turn can chain several
// round trips, and the default 100s cuts long answers off mid-generation.
builder.Services.AddHttpClient("assistant", client =>
{
    client.Timeout = TimeSpan.FromMinutes(3);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SMSNet-PakDedi/1.0");
});

builder.Services.AddSingleton<MarkdownRenderer>();
builder.Services.AddSingleton<AssistantKernelFactory>();
builder.Services.AddSingleton<AssistantService>();
builder.Services.AddScoped<ChatUploadService>();

// The error page surfaces the trace identifier so a report can be matched to a log line.
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    var seeder = new DbInitializer(
        dbContext,
        scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>(),
        scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>());
    await seeder.SeedAsync();
}

app.Run();
