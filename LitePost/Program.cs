using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using LitePost.Data;
using LitePost.Data.Models;
using LitePost.Data.Services;
using LitePost.Auth;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddScoped<ProtectedSessionStorage>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// Database Context Factory (Best practice for Blazor Server)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=litepost.db";
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(connectionString));

// Services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PostService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<ListService>();
builder.Services.AddScoped<MessageService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// Seed Data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var contextFactory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var context = contextFactory.CreateDbContext();
    
    // Ensure DB Created
    context.Database.EnsureCreated();

    if (!context.Users.Any())
    {
        // Add Admin
        var admin = new User
        {
            Username = "admin",
            PasswordHash = "jGl25bVBBBW96Qi9Te4V37Fnqchz/Eu4qB9vKrRIqRg=", // SHA256 for "admin"
            DisplayName = "Administrator",
            IsAdmin = true,
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(admin);
        context.SaveChanges();
    }
}

app.Run();