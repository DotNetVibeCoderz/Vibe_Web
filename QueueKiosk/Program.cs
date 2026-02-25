using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using QueueKiosk.Data;
using QueueKiosk.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=queue.db"));

// Auth Setup
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(p => p.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<AuthService>();

// Domain Services
builder.Services.AddSingleton<NotificationMockService>();
builder.Services.AddSingleton<QueueManagerService>(); // Stateful for signalr broadcast

// Swagger/API setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Migrate and Seed Database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Minimal API Setup
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "QueueKiosk API v1"));

app.UseAuthentication();
app.UseAuthorization();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// API Endpoint Definition with API Key Auth
app.MapGet("/api/queue", async (HttpContext context, AppDbContext db) =>
{
    if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey) || extractedApiKey != "MySecretKey")
        return Results.Unauthorized();

    var queues = await db.QueueTickets.Include(q => q.Service).ToListAsync();
    return Results.Ok(queues);
});

app.Run();
