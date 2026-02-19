using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SimpleDMS.Data;
using SimpleDMS.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=simpledms.db";
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connectionString, o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 4;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<AppDbContext>();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Storage Config
var storageType = builder.Configuration["StorageType"] ?? "FileSystem";
if (storageType == "FileSystem")
{
    builder.Services.AddSingleton<IStorageService>(new FileSystemStorageService(Path.Combine(builder.Environment.ContentRootPath, "Storage")));
}

builder.Services.AddScoped<DocumentService>();

// Swagger & API
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
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

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// --- Minimal API Endpoints ---
var api = app.MapGroup("/api/documents");

api.MapGet("/", async (DocumentService docService, Guid? folderId) => {
    var root = await docService.GetOrCreateRootFolder();
    return Results.Ok(await docService.GetDocumentsAsync(folderId ?? root.Id));
});

// Download endpoint
app.MapGet("/api/download/{versionId}", async (Guid versionId, AppDbContext db, DocumentService docService) => {
    var version = await db.DocumentVersions.FindAsync(versionId);
    if (version == null) return Results.NotFound();
    
    var stream = await docService.GetFileStreamAsync(version.FilePath);
    return Results.File(stream, version.ContentType, version.FileName);
});

app.UseSwagger();
app.UseSwaggerUI();

// --- Seed Data ---
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    
    string[] roles = { "Admin", "Editor", "Viewer" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    if (await userManager.FindByNameAsync("admin") == null)
    {
        var admin = new ApplicationUser { UserName = "admin", Email = "admin@simpledms.com", FullName = "Administrator", EmailConfirmed = true };
        await userManager.CreateAsync(admin, "admin");
        await userManager.AddToRoleAsync(admin, "Admin");
    }
}

app.Run();
