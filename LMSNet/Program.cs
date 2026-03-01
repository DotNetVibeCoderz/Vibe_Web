using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using LMSNet.Data;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=lmsnet.db"));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Identity Setup
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddScoped<AuthenticationStateProvider, ServerAuthenticationStateProvider>();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

builder.Services.AddIdentityCore<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddSignInManager()
.AddDefaultTokenProviders();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

app.UseAntiforgery();

// Endpoint for Login POST request (full page post untuk set cookie)
app.MapPost("/Account/Login", async (
    HttpContext httpContext,
    SignInManager<ApplicationUser> signInManager) =>
{
    if (!httpContext.Request.HasFormContentType)
    {
        return Results.LocalRedirect("/Account/Login?error=1");
    }

    var form = await httpContext.Request.ReadFormAsync();
    var email = form["Email"].ToString();
    var password = form["Password"].ToString();
    var returnUrl = form["ReturnUrl"].ToString();

    var rememberRaw = form["RememberMe"].ToString();
    var rememberMe = string.Equals(rememberRaw, "true", StringComparison.OrdinalIgnoreCase)
        || string.Equals(rememberRaw, "on", StringComparison.OrdinalIgnoreCase);

    if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
    {
        return Results.LocalRedirect("/Account/Login?error=1");
    }

    var result = await signInManager.PasswordSignInAsync(
        email,
        password,
        rememberMe,
        lockoutOnFailure: false);

    if (result.Succeeded)
    {
        var redirectUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
        return Results.LocalRedirect(redirectUrl);
    }

    return Results.LocalRedirect("/Account/Login?error=1");
}).DisableAntiforgery().WithOrder(-1);

app.MapRazorComponents<LMSNet.Components.App>()
    .AddInteractiveServerRenderMode();

// Endpoint for Logout POST request
app.MapPost("/Account/Logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.LocalRedirect("/");
});

// Configure API endpoints
app.MapGet("/api/courses", async (ApplicationDbContext db) =>
{
    return await db.Courses.Select(c => new { c.Id, c.Title, c.Description, c.Price, c.Status }).ToListAsync();
}).WithName("GetCourses");

// Seed Default Data and Database Initialization
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    dbContext.Database.EnsureCreated();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    // 1. Seed Roles
    string[] roles = { "admin", "mentor", "student", "creator" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // 2. Seed Users
    // Admin
    var adminUser = await userManager.FindByEmailAsync("admin@lmsnet.com");
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = "admin@lmsnet.com",
            Email = "admin@lmsnet.com",
            FirstName = "Super",
            LastName = "Admin",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, "admin");
    }

    // Mentor
    var mentorUser = await userManager.FindByEmailAsync("jacky@lmsnet.com");
    if (mentorUser == null)
    {
        mentorUser = new ApplicationUser
        {
            UserName = "jacky@lmsnet.com",
            Email = "jacky@lmsnet.com",
            FirstName = "Jacky",
            LastName = "Bender",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(mentorUser, "Mentor123!");
        await userManager.AddToRoleAsync(mentorUser, "mentor");
    }

    // Student
    var studentUser = await userManager.FindByEmailAsync("student@lmsnet.com");
    if (studentUser == null)
    {
        studentUser = new ApplicationUser
        {
            UserName = "student@lmsnet.com",
            Email = "student@lmsnet.com",
            FirstName = "Budi",
            LastName = "Santoso",
            EmailConfirmed = true
        };
        await userManager.CreateAsync(studentUser, "Student123!");
        await userManager.AddToRoleAsync(studentUser, "student");
    }

    // 3. Seed Courses
    if (!dbContext.Courses.Any())
    {
        var sampleCourses = new List<Course>
        {
            new Course
            {
                Title = "Mastering C# and .NET 10",
                Description = "A complete guide to mastering C# 13 and .NET 10 with Jacky the Code Bender.",
                Price = 49.99m,
                Status = "Published",
                InstructorId = adminUser.Id,
                Modules = new List<Module>
                {
                    new Module
                    {
                        Title = "Introduction to .NET 10",
                        OrderIndex = 1,
                        Lessons = new List<Lesson>
                        {
                            new Lesson { Title = "What is .NET 10?", Content = "Overview of .NET 10 features.", OrderIndex = 1 },
                            new Lesson { Title = "Setting up the environment", Content = "Install Visual Studio 2022 and SDK.", OrderIndex = 2 }
                        }
                    },
                    new Module
                    {
                        Title = "Advanced C# Features",
                        OrderIndex = 2,
                        Lessons = new List<Lesson>
                        {
                            new Lesson { Title = "Primary Constructors", Content = "Learning primary constructors in C#.", OrderIndex = 1 },
                            new Lesson { Title = "Collection Expressions", Content = "How to use collection expressions efficiently.", OrderIndex = 2 }
                        }
                    }
                }
            },
            new Course
            {
                Title = "Fullstack Web Development with NodeJS",
                Description = "Learn how to build scalable web applications using Express, NestJS, and React.",
                Price = 35.50m,
                Status = "Published",
                InstructorId = mentorUser.Id,
                Modules = new List<Module>
                {
                    new Module
                    {
                        Title = "Backend with Express",
                        OrderIndex = 1,
                        Lessons = new List<Lesson>
                        {
                            new Lesson { Title = "NodeJS Runtime", Content = "Understanding the V8 engine.", OrderIndex = 1 },
                            new Lesson { Title = "REST API with Express", Content = "Building your first API endpoint.", OrderIndex = 2 }
                        }
                    }
                }
            },
            new Course
            {
                Title = "Rust for Systems Programming",
                Description = "Deep dive into memory safety, ownership, and concurrency with Rust.",
                Price = 55.00m,
                Status = "Published",
                InstructorId = mentorUser.Id,
                Modules = new List<Module>
                {
                    new Module
                    {
                        Title = "Rust Fundamentals",
                        OrderIndex = 1,
                        Lessons = new List<Lesson>
                        {
                            new Lesson { Title = "Ownership and Borrowing", Content = "The core concept of Rust memory safety.", OrderIndex = 1 },
                            new Lesson { Title = "Structs and Enums", Content = "Defining data structures.", OrderIndex = 2 }
                        }
                    }
                }
            },
            new Course
            {
                Title = "Python for AI and Data Science",
                Description = "Unlock the power of AI using Python, NumPy, Pandas, and TensorFlow.",
                Price = 45.00m,
                Status = "Draft",
                InstructorId = adminUser.Id,
                Modules = new List<Module>
                {
                    new Module
                    {
                        Title = "Data Analysis",
                        OrderIndex = 1,
                        Lessons = new List<Lesson>
                        {
                            new Lesson { Title = "Pandas DataFrames", Content = "Manipulating data with Pandas.", OrderIndex = 1 }
                        }
                    }
                }
            },
            new Course
            {
                Title = "Go (Golang) Microservices",
                Description = "Building fast and efficient microservices with Go and gRPC.",
                Price = 39.99m,
                Status = "Published",
                InstructorId = mentorUser.Id,
                Modules = new List<Module>
                {
                    new Module
                    {
                        Title = "Go Concurrency",
                        OrderIndex = 1,
                        Lessons = new List<Lesson>
                        {
                            new Lesson { Title = "Goroutines and Channels", Content = "Mastering concurrency in Go.", OrderIndex = 1 }
                        }
                    }
                }
            }
        };

        dbContext.Courses.AddRange(sampleCourses);
        dbContext.SaveChanges();
    }
}

app.Run();
