using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using NetTube.Components;
using NetTube.Data;
using NetTube.Models;
using NetTube.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Configure SQLite Database
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Application Services
builder.Services.AddScoped<VideoService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(p => p.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddAuthorizationCore();
builder.Services.AddCascadingAuthenticationState();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Ensure database is created and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var contextFactory = services.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var context = contextFactory.CreateDbContext();
    
    // We recreate for testing purposes so everything is fresh and clean based on new schema
    context.Database.EnsureDeleted();
    context.Database.EnsureCreated();

    if (!context.Users.Any())
    {
        // 1. Seed Users
        var adminUser = new User 
        { 
            Username = "admin", 
            Email = "admin@nettube.com", 
            PasswordHash = "admin123", 
            ProfilePictureUrl = "https://i.pravatar.cc/150?u=admin"
        };
        
        var creatorUser = new User 
        { 
            Username = "BlenderFoundation", 
            Email = "blender@nettube.com", 
            PasswordHash = "blender123", 
            ProfilePictureUrl = "https://i.pravatar.cc/150?u=blender"
        };

        context.Users.AddRange(adminUser, creatorUser);
        context.SaveChanges();

        // 2. Seed Subscriptions
        context.Subscriptions.Add(new Subscription { SubscriberId = adminUser.Id, CreatorId = creatorUser.Id });
        context.SaveChanges();

        // 3. Seed Videos
        var video1 = new Video 
        {
            Title = "Big Buck Bunny", 
            Description = "A large and lovable rabbit deals with three bullying rodents.", 
            VideoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4", 
            ThumbnailUrl = "https://images.unsplash.com/photo-1583337130417-3346a1be7dee?w=600&q=80",
            Category = "Movies",
            UserId = creatorUser.Id,
            Views = 24500,
            UploadedAt = DateTime.UtcNow.AddDays(-10)
        };

        var video2 = new Video 
        {
            Title = "Elephants Dream", 
            Description = "The first computer-generated short film produced almost entirely using free and open-source software.", 
            VideoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/ElephantsDream.mp4", 
            ThumbnailUrl = "https://images.unsplash.com/photo-1557050543-4d5f4e07ef46?w=600&q=80",
            Category = "Movies",
            UserId = creatorUser.Id,
            Views = 12050,
            UploadedAt = DateTime.UtcNow.AddDays(-5)
        };
        
        var video3 = new Video 
        {
            Title = "Sintel", 
            Description = "A lonely young woman, Sintel, helps and befriends a dragon.", 
            VideoUrl = "http://commondatastorage.googleapis.com/gtv-videos-bucket/sample/Sintel.mp4", 
            ThumbnailUrl = "https://upload.wikimedia.org/wikipedia/commons/thumb/8/8f/Sintel_poster.jpg/250px-Sintel_poster.jpg?w=600&q=80",
            Category = "Movies",
            UserId = creatorUser.Id,
            Views = 89000,
            UploadedAt = DateTime.UtcNow.AddDays(-2)
        };

        context.Videos.AddRange(video1, video2, video3);
        context.SaveChanges();

        // 4. Seed Playlists
        var playlist1 = new Playlist
        {
            Title = "My Favorite Open Movies",
            UserId = adminUser.Id,
        };
        context.Playlists.Add(playlist1);
        context.SaveChanges();

        context.PlaylistVideos.Add(new PlaylistVideo { PlaylistId = playlist1.Id, VideoId = video1.Id });
        context.PlaylistVideos.Add(new PlaylistVideo { PlaylistId = playlist1.Id, VideoId = video3.Id });
        context.SaveChanges();

        // 5. Seed Watch History
        context.WatchHistories.Add(new WatchHistory { UserId = adminUser.Id, VideoId = video1.Id, WatchedAt = DateTime.UtcNow.AddHours(-5) });
        context.WatchHistories.Add(new WatchHistory { UserId = adminUser.Id, VideoId = video2.Id, WatchedAt = DateTime.UtcNow.AddHours(-1) });
        context.SaveChanges();

        // 6. Seed Comments
        context.Comments.Add(new Comment { UserId = adminUser.Id, VideoId = video1.Id, Content = "This is a classic masterpiece!" });
        context.Comments.Add(new Comment { UserId = creatorUser.Id, VideoId = video1.Id, Content = "Thank you so much!" });
        context.SaveChanges();
    }
}

app.Run();
