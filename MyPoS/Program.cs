using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using MyPoS.Data;
using Microsoft.AspNetCore.Components.Authorization;
using MyPoS.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration for Storage
var storageConfig = builder.Configuration.GetSection("Storage");
builder.Services.Configure<StorageConfig>(storageConfig);

// Register Storage Service based on config
var provider = storageConfig.GetValue<string>("Provider") ?? "FileSystem";
if (provider.Equals("AzureBlob", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
}
else if (provider.Equals("AWSS3", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddScoped<IStorageService, AwsS3StorageService>();
}
else
{
    builder.Services.AddScoped<IStorageService, FileSystemStorageService>();
}

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// Add DbContext
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite("Data Source=mypos.db"));

// Auth
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ExportService>();

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

// Need to ensure DB created
using (var scope = app.Services.CreateScope())
{
    var dbContextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
    using var dbContext = dbContextFactory.CreateDbContext();
    try 
    {
        // Test if column ImageUrl exists in Product
        dbContext.Products.FirstOrDefault();
    }
    catch 
    {
        // Recreate db if schema changed
        dbContext.Database.EnsureDeleted();
    }
    dbContext.Database.EnsureCreated();
}

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
