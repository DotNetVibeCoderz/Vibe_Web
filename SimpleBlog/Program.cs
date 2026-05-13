using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using SimpleBlog.Data;
using SimpleBlog.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Identity;
using SimpleBlog.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Database Selection Logic
var dbProvider = builder.Configuration["DatabaseSettings:Provider"];
var dbConnectionString = builder.Configuration["DatabaseSettings:ConnectionString"];

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (dbProvider == "SqlServer")
    {
        options.UseSqlServer(dbConnectionString);
    }
    else if (dbProvider == "MySql")
    {
        options.UseMySql(dbConnectionString, ServerVersion.AutoDetect(dbConnectionString));
    }
    else // Default to Sqlite
    {
        options.UseSqlite(dbConnectionString ?? "Data Source=SimpleBlog.db");
    }
});

// Identity with Roles
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders()
.AddDefaultUI();

// Storage Service Selection Logic
var storageProvider = builder.Configuration["StorageSettings:Provider"];
if (storageProvider == "Azure")
{
    builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
}
else if (storageProvider == "S3")
{
    builder.Services.AddScoped<IStorageService, S3StorageService>();
}
else
{
    builder.Services.AddScoped<IStorageService, FileSystemStorageService>();
}

builder.Services.AddScoped<BlogService>();

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
app.MapRazorPages(); // Needed for Identity UI

// Seed Data & Roles
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<ApplicationDbContext>();
    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    
    db.Database.EnsureCreated();

    // Seed Roles
    string[] roleNames = { "Admin", "Editor" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // Seed Admin User
    var adminEmail = "admin@gravicode.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new IdentityUser { UserName = adminEmail, Email = adminEmail, EmailConfirmed = true };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "Admin");
    }

    if (!db.Categories.Any())
    {
        var catEng = new Category { Name = "Engineering", Slug = "engineering", Icon = "⚙️" };
        var catAI = new Category { Name = "Artificial Intelligence", Slug = "ai", Icon = "🤖" };
        var catLife = new Category { Name = "Life at Gravicode", Slug = "life", Icon = "☕" };
        
        db.Categories.AddRange(catEng, catAI, catLife);

        var tagDotnet = new Tag { Name = "dotnet" };
        var tagBlazor = new Tag { Name = "blazor" };
        var tagRust = new Tag { Name = "rust" };
        db.Tags.AddRange(tagDotnet, tagBlazor, tagRust);

        var post1 = new BlogPost 
        { 
            Title = "Building High Performance APIs with Rust", 
            Slug = "rust-apis", 
            Summary = "Why we started moving some of our microservices from Node.js to Rust for better memory safety.",
            Content = "Rust is amazing and fast!", 
            IsPublished = true, 
            Category = catEng,
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Likes = 42
        };

        db.BlogPosts.Add(post1);
        db.SaveChanges();
    }
}

app.Run();
