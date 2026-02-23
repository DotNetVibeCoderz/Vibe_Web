using Kopdar.Components;
using Kopdar.Data;
using Kopdar.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// BLAZOR COOKIE Auth Code (begin)

builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});
builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<HttpContextAccessor>();
builder.Services.AddHttpClient();
builder.Services.AddScoped<HttpClient>();
// BLAZOR COOKIE Auth Code (end)

// Database Provider Selection
var dbProvider = builder.Configuration["DatabaseProvider"] ?? "Sqlite";
builder.Services.AddDbContextFactory<AppDbContext>(options =>
{
    if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        var conn = builder.Configuration.GetConnectionString("SqlServerConnection");
        options.UseSqlServer(conn);
    }
    else if (dbProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        var conn = builder.Configuration.GetConnectionString("MySqlConnection");
        options.UseMySql(conn, ServerVersion.AutoDetect(conn));
    }
    else // Default to Sqlite
    {
        var conn = builder.Configuration.GetConnectionString("SqliteConnection");
        options.UseSqlite(conn);
    }
});

// Storage Provider Injection Strategy
var storageProvider = builder.Configuration["StorageProvider"];
if (storageProvider == "AzureBlob")
{
    builder.Services.AddSingleton<IStorageService, AzureBlobStorageService>();
}
else if (storageProvider == "AwsS3")
{
    builder.Services.AddSingleton<IStorageService, AwsS3StorageService>();
}
else // Default fallback to FileSystem
{
    builder.Services.AddSingleton<IStorageService, LocalStorageService>();
}
builder.Services.AddScoped<AppService>();
builder.Services.AddSignalR(hubOptions =>
{
    hubOptions.MaximumReceiveMessageSize = 128 * 1024; // 1MB
});
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// ******
// BLAZOR COOKIE Auth Code (begin)
app.UseRouting();
app.UseCookiePolicy();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // enable cshtml pages
// BLAZOR COOKIE Auth Code (end)

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// DB Init & Seed Extensive Data
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var context = dbFactory.CreateDbContext();
    
    // Refresh Schema & DB
    context.Database.EnsureCreated();
    
    if (!context.Users.Any())
    {
        var user1 = new Kopdar.Models.AppUser { Username = "kangfadhil", Email = "fadhil@gravicode.com", PasswordHash = "admin", Latitude = -6.2, Longitude = 106.8, ProfilePictureUrl = "/images/logo.svg", Bio = "Code Bender at Gravicode Studios 💻🔥", Gender = "Male" };
        var user2 = new Kopdar.Models.AppUser { Username = "jacky", Email = "jacky@kopdar.id", PasswordHash = "admin", Latitude = -6.21, Longitude = 106.81, Bio = "Fullstack Developer. Suka kopi ☕ dan coding semalaman 🌙.", Gender = "Male", ProfilePictureUrl = "https://cdn-icons-png.flaticon.com/512/4140/4140048.png" };
        var user3 = new Kopdar.Models.AppUser { Username = "budi", Email = "budi@kopdar.id", PasswordHash = "admin", Latitude = -6.22, Longitude = 106.82, Bio = "Gamer sejati. Main Valorant dan Dota 2 🎮", Gender = "Male", ProfilePictureUrl = "https://cdn-icons-png.flaticon.com/512/4140/4140048.png" };
        var user4 = new Kopdar.Models.AppUser { Username = "siti", Email = "siti@kopdar.id", PasswordHash = "admin", Latitude = -6.25, Longitude = 106.85, Bio = "Suka masak masakan nusantara 🍳. Traveller ✈️.", Gender = "Female", ProfilePictureUrl = "https://cdn-icons-png.flaticon.com/512/4140/4140047.png" };
        var user5 = new Kopdar.Models.AppUser { Username = "gamer_pro", Email = "gamer@kopdar.id", PasswordHash = "admin", Latitude = -6.18, Longitude = 106.82, Bio = "Hardcore gamer, jago Valorant. Fullstack dev at night.", Gender = "Male", ProfilePictureUrl = "https://cdn-icons-png.flaticon.com/512/4140/4140048.png" };
        var user6 = new Kopdar.Models.AppUser { Username = "lisa_blackpink", Email = "lisa@kopdar.id", PasswordHash = "admin", Latitude = -6.15, Longitude = 106.75, Bio = "Pecinta musik K-pop 🎧, dancer 💃.", Gender = "Female", ProfilePictureUrl = "https://cdn-icons-png.flaticon.com/512/4140/4140047.png" };
        var user7 = new Kopdar.Models.AppUser { Username = "kopi_senja", Email = "kopi@kopdar.id", PasswordHash = "admin", Latitude = -6.28, Longitude = 106.88, Bio = "Anak indie suka kopi senja ☕ dan baca buku puisi 📖.", Gender = "Male", ProfilePictureUrl = "https://cdn-icons-png.flaticon.com/512/4140/4140048.png" };

        context.Users.AddRange(user1, user2, user3, user4, user5, user6, user7);
        context.SaveChanges();

        // Seed Groups
        var group1 = new Kopdar.Models.ChatGroup { Name = "Gravicode Studios", Description = "Developer Community and Software Engineering Talk 🚀", CreatorId = user1.Id, Latitude = -6.2, Longitude = 106.8, IsPublic = true, GroupPictureUrl = "https://cdn-icons-png.flaticon.com/512/32/32441.png" };
        var group2 = new Kopdar.Models.ChatGroup { Name = "Gamer Jakarta", Description = "Mabar Valorant, ML, PUBG, Dota 2 🎮🔥", CreatorId = user3.Id, Latitude = -6.22, Longitude = 106.82, IsPublic = true, GroupPictureUrl = "https://cdn-icons-png.flaticon.com/512/32/32441.png" };
        var group3 = new Kopdar.Models.ChatGroup { Name = "Pecinta Kopi Nusantara", Description = "Membahas kopi dari seluruh penjuru Indonesia ☕🇮🇩", CreatorId = user7.Id, Latitude = -6.28, Longitude = 106.88, IsPublic = true, GroupPictureUrl = "https://cdn-icons-png.flaticon.com/512/32/32441.png" };

        context.ChatGroups.AddRange(group1, group2, group3);
        context.SaveChanges();

        // Group Memberships
        context.GroupMembers.AddRange(
            new Kopdar.Models.GroupMember { GroupId = group1.Id, UserId = user1.Id },
            new Kopdar.Models.GroupMember { GroupId = group1.Id, UserId = user2.Id },
            new Kopdar.Models.GroupMember { GroupId = group2.Id, UserId = user3.Id },
            new Kopdar.Models.GroupMember { GroupId = group2.Id, UserId = user5.Id },
            new Kopdar.Models.GroupMember { GroupId = group3.Id, UserId = user7.Id },
            new Kopdar.Models.GroupMember { GroupId = group3.Id, UserId = user4.Id }
        );
        context.SaveChanges();

        // Seed Posts with emojis, empty mediaUrls, etc
        var posts = new List<Kopdar.Models.Post>
        {
            new Kopdar.Models.Post { UserId = user1.Id, Content = "Welcome to Kopdar! The coolest social and messaging app in neo-brutalism design! 🚀🔥", MediaUrl = "", CreatedAt = DateTime.UtcNow.AddHours(-2) },
            new Kopdar.Models.Post { UserId = user2.Id, Content = "Lagi pusing nge-debug code dari pagi, belum kelar juga. Butuh kopi ☕", MediaUrl = "https://images.unsplash.com/photo-1497935586351-b67a49e012bf?w=500", CreatedAt = DateTime.UtcNow.AddMinutes(-45) },
            new Kopdar.Models.Post { UserId = user3.Id, Content = "Ada yang main Valorant malem ini? Push rank yuk! 🎮", MediaUrl = "", CreatedAt = DateTime.UtcNow.AddMinutes(-30) },
            new Kopdar.Models.Post { UserId = user4.Id, Content = "Resep nasi goreng seafood hari ini sukses besar! 🦐🍚", MediaUrl = "https://images.unsplash.com/photo-1512058564366-18510be2db19?w=500", CreatedAt = DateTime.UtcNow.AddMinutes(-15) },
            new Kopdar.Models.Post { UserId = user6.Id, Content = "Dance practice today was exhausting but fun! 💃✨", MediaUrl = "", CreatedAt = DateTime.UtcNow.AddMinutes(-10) }
        };
        context.Posts.AddRange(posts);
        context.SaveChanges();

        // Seed Comments
        var comments = new List<Kopdar.Models.Comment>
        {
            new Kopdar.Models.Comment { PostId = posts[1].Id, UserId = user1.Id, Content = "Semangat bro! Istirahat dulu gih. 👍" },
            new Kopdar.Models.Comment { PostId = posts[1].Id, UserId = user7.Id, Content = "Kopi arabica gayo mantap buat nemenin coding bro. ☕" },
            new Kopdar.Models.Comment { PostId = posts[2].Id, UserId = user5.Id, Content = "Gass login sekarang bang! Gw udah di lobby. 🔥" }
        };
        context.Comments.AddRange(comments);
        context.SaveChanges();

        // Seed Follows
        var follows = new List<Kopdar.Models.Follow>
        {
            new Kopdar.Models.Follow { FollowerId = user2.Id, FollowingId = user1.Id },
            new Kopdar.Models.Follow { FollowerId = user3.Id, FollowingId = user5.Id },
            new Kopdar.Models.Follow { FollowerId = user5.Id, FollowingId = user3.Id },
            new Kopdar.Models.Follow { FollowerId = user7.Id, FollowingId = user4.Id },
            new Kopdar.Models.Follow { FollowerId = user1.Id, FollowingId = user2.Id }
        };
        context.Follows.AddRange(follows);
        context.SaveChanges();

        // Seed Messages
        var msgs = new List<Kopdar.Models.Message>
        {
            new Kopdar.Models.Message { SenderId = user1.Id, ReceiverId = user2.Id, Content = "Halo bro, lagi sibuk apa?", SentAt = DateTime.UtcNow.AddMinutes(-30), MediaUrl = "" },
            new Kopdar.Models.Message { SenderId = user2.Id, ReceiverId = user1.Id, Content = "Lagi ngerjain project Kopdar nih bang, bentar lagi kelar 😁", SentAt = DateTime.UtcNow.AddMinutes(-25), MediaUrl = "" },
            new Kopdar.Models.Message { SenderId = user3.Id, ReceiverId = user5.Id, Content = "Bro masuk discord sekarang, mau mulai nih gamenya 🎮", SentAt = DateTime.UtcNow.AddMinutes(-5), MediaUrl = "" }
        };
        context.Messages.AddRange(msgs);

        // Seed Notifications
        context.Notifications.AddRange(
            new Kopdar.Models.Notification { UserId = user1.Id, Content = "**jacky** liked your post.", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-20) },
            new Kopdar.Models.Notification { UserId = user1.Id, Content = "**budi** started following you.", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-10) },
            new Kopdar.Models.Notification { UserId = user1.Id, Content = "New message from **jacky**.", IsRead = false, CreatedAt = DateTime.UtcNow.AddMinutes(-2) }
        );
        context.SaveChanges();
    }
}

app.Run();
