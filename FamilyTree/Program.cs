using FamilyTree.Components;
using FamilyTree.Data;
using FamilyTree.Models;
using FamilyTree.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var databaseProvider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var sqliteConnection = builder.Configuration.GetConnectionString("Sqlite");
var sqlServerConnection = builder.Configuration.GetConnectionString("SqlServer");

void ConfigureDb(DbContextOptionsBuilder options)
{
    if (databaseProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        options.UseSqlServer(sqlServerConnection);
    }
    else
    {
        options.UseSqlite(sqliteConnection);
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(ConfigureDb);
builder.Services.AddDbContextFactory<ApplicationDbContext>(ConfigureDb, ServiceLifetime.Scoped);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuthenticationStateProvider, HttpContextAuthenticationStateProvider>();

builder.Services.AddDataProtection();
builder.Services.AddScoped<IEncryptionService, DataProtectionEncryptionService>();

builder.Services.Configure<StorageSettings>(builder.Configuration.GetSection("Storage"));
builder.Services.AddScoped<StorageFactory>();
builder.Services.AddScoped<FileSystemStorage>();
builder.Services.AddScoped<AzureBlobStorage>();
builder.Services.AddScoped<AwsS3Storage>();

builder.Services.AddScoped<TreeService>();
builder.Services.AddScoped<StatsService>();
builder.Services.AddScoped<ExportService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapPost("/account/login/submit", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager) =>
    {
        var form = await httpContext.Request.ReadFormAsync();
        var username = form["username"].ToString();
        var password = form["password"].ToString();
        var result = await signInManager.PasswordSignInAsync(username, password, true, lockoutOnFailure: false);

        if (result.Succeeded)
        {
            httpContext.Response.Redirect("/");
        }
        else
        {
            httpContext.Response.Redirect("/account/login?error=1");
        }
    })
    .DisableAntiforgery();

app.MapGet("/account/logout", async (HttpContext httpContext, SignInManager<ApplicationUser> signInManager) =>
    {
        await signInManager.SignOutAsync();
        httpContext.Response.Redirect("/account/login");
    });

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await DbInitializer.SeedAsync(app.Services);

app.Run();
