using Intranet.Models;
using Microsoft.EntityFrameworkCore;

namespace Intranet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<NewsItem> NewsItems => Set<NewsItem>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<SocialPost> SocialPosts => Set<SocialPost>();
    public DbSet<SocialComment> SocialComments => Set<SocialComment>();
    public DbSet<SocialLike> SocialLikes => Set<SocialLike>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<AppItem> Apps => Set<AppItem>();
    public DbSet<WikiArticle> WikiArticles => Set<WikiArticle>();
    public DbSet<FaqItem> Faqs => Set<FaqItem>();
    public DbSet<ForumTopic> ForumTopics => Set<ForumTopic>();
    public DbSet<ForumPost> ForumPosts => Set<ForumPost>();
    public DbSet<Reward> Rewards => Set<Reward>();
    public DbSet<Survey> Surveys => Set<Survey>();
    public DbSet<SurveyOption> SurveyOptions => Set<SurveyOption>();
    public DbSet<UserVote> UserVotes => Set<UserVote>();
    public DbSet<Album> Albums => Set<Album>();
    public DbSet<MediaItem> MediaItems => Set<MediaItem>();
    public DbSet<MediaComment> MediaComments => Set<MediaComment>();
    public DbSet<MediaLike> MediaLikes => Set<MediaLike>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Seed Users
        modelBuilder.Entity<User>().HasData(
            new User { Id = 1, Username = "admin", PasswordHash = "admin123", FullName = "System Administrator", Email = "admin@intranet.local", Role = "admin", Department = "IT", JobTitle = "IT Manager", AvatarUrl = "https://i.pravatar.cc/150?u=admin" },
            new User { Id = 2, Username = "creator", PasswordHash = "creator123", FullName = "Content Creator", Email = "creator@intranet.local", Role = "creator", Department = "HR", JobTitle = "HR Specialist", AvatarUrl = "https://i.pravatar.cc/150?u=creator" },
            new User { Id = 3, Username = "user", PasswordHash = "user123", FullName = "Regular User", Email = "user@intranet.local", Role = "user", Department = "Sales", JobTitle = "Sales Rep", AvatarUrl = "https://i.pravatar.cc/150?u=user" }
        );

        for (int i = 4; i <= 20; i++)
        {
            modelBuilder.Entity<User>().HasData(
                new User { Id = i, Username = $"user{i}", PasswordHash = "password", FullName = $"Employee {i}", Email = $"employee{i}@intranet.local", Role = "user", Department = (i % 2 == 0) ? "Engineering" : "Marketing", JobTitle = "Staff", AvatarUrl = $"https://i.pravatar.cc/150?u={i}" }
            );
        }

        modelBuilder.Entity<Department>().HasData(
            new Department { Id = 1, Name = "IT", Description = "Information Technology" },
            new Department { Id = 2, Name = "HR", Description = "Human Resources" },
            new Department { Id = 3, Name = "Sales", Description = "Sales and Marketing" },
            new Department { Id = 4, Name = "Engineering", Description = "Product Engineering" }
        );

        modelBuilder.Entity<NewsItem>().HasData(
            new NewsItem { Id = 1, Title = "Welcome to our new Intranet!", Content = "<p>We are thrilled to launch the new company intranet.</p>", CreatedAt = DateTime.UtcNow.AddDays(-5), Author = "admin", Category = "Announcement", CoverImageUrl = "https://placehold.co/600x400/1e3a8a/ffffff?text=Welcome" },
            new NewsItem { Id = 2, Title = "Q3 Townhall Meeting", Content = "<p>Join us for the Q3 townhall next friday.</p>", CreatedAt = DateTime.UtcNow.AddDays(-2), Author = "admin", Category = "Leadership", CoverImageUrl = "https://placehold.co/600x400/1e3a8a/ffffff?text=Townhall" }
        );

        modelBuilder.Entity<AppSetting>().HasData(
            new AppSetting { Id = 1, Key = "LogoUrl", Value = "https://placehold.co/200x50/png?text=LOGO" },
            new AppSetting { Id = 2, Key = "PortalTitle", Value = "Corporate Intranet" },
            new AppSetting { Id = 3, Key = "PortalDescription", Value = "Welcome to the Digital Workplace" },
            new AppSetting { Id = 4, Key = "ThemeColor", Value = "blue" }
        );
        
        modelBuilder.Entity<SocialPost>().HasData(
            new SocialPost { Id = 1, Content = "Hello everyone! Loving the new portal.", CreatedAt = DateTime.UtcNow.AddDays(-1), Author = "creator", AuthorAvatar = "https://i.pravatar.cc/150?u=creator", Likes = 5 },
            new SocialPost { Id = 2, Content = "Don't forget the team building event tomorrow!", CreatedAt = DateTime.UtcNow.AddHours(-5), Author = "admin", AuthorAvatar = "https://i.pravatar.cc/150?u=admin", Likes = 12 }
        );

        modelBuilder.Entity<Project>().HasData(
            new Project { Id = 1, Name = "Website Redesign", Description = "Revamping the corporate website", StartDate = DateTime.UtcNow.AddDays(-10), Status = "Active" }
        );

        modelBuilder.Entity<TaskItem>().HasData(
            new TaskItem { Id = 1, ProjectId = 1, Title = "Design Mockups", Description = "Create Figma mockups", Assignee = "creator", Status = "InProgress", DueDate = DateTime.UtcNow.AddDays(5) },
            new TaskItem { Id = 2, ProjectId = 1, Title = "Setup Server", Description = "Provision AWS resources", Assignee = "admin", Status = "Todo", DueDate = DateTime.UtcNow.AddDays(10) }
        );

        modelBuilder.Entity<AppItem>().HasData(
            new AppItem { Id = 1, Name = "HR Portal", Description = "Leave and attendance management", Url = "https://hr.intranet.local", IconUrl = "fas fa-users-cog", Category = "Human Resources" },
            new AppItem { Id = 2, Name = "Expense Claim", Description = "Submit expense reports", Url = "https://finance.intranet.local", IconUrl = "fas fa-file-invoice-dollar", Category = "Finance" }
        );

        modelBuilder.Entity<WikiArticle>().HasData(
            new WikiArticle { Id = 1, Title = "Onboarding Guide", Content = "<h2>Welcome to the company!</h2><p>Here are your first steps...</p>", Author = "admin", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        );

        modelBuilder.Entity<FaqItem>().HasData(
            new FaqItem { Id = 1, Question = "How do I reset my password?", Answer = "Contact the IT Helpdesk at helpdesk@intranet.local.", Category = "IT Support" },
            new FaqItem { Id = 2, Question = "What is the policy for remote work?", Answer = "Employees can work remotely up to 2 days a week with manager approval.", Category = "HR Policy" }
        );

        modelBuilder.Entity<ForumTopic>().HasData(
            new ForumTopic { Id = 1, Title = "Suggestions for next team building?", Content = "Any ideas for our Q4 team building?", Author = "creator", CreatedAt = DateTime.UtcNow.AddDays(-2), Category = "General", Likes = 3 }
        );

        modelBuilder.Entity<ForumPost>().HasData(
            new ForumPost { Id = 1, ForumTopicId = 1, Content = "How about an escape room?", Author = "user", CreatedAt = DateTime.UtcNow.AddDays(-1) }
        );

        modelBuilder.Entity<Reward>().HasData(
            new Reward { Id = 1, Receiver = "user", Sender = "admin", Message = "Great job on the recent project deployment!", BadgeIcon = "fas fa-star", CreatedAt = DateTime.UtcNow.AddDays(-3) }
        );

        modelBuilder.Entity<Survey>().HasData(
            new Survey { Id = 1, Title = "Office Lunch Preferences", Description = "What kind of food should we cater for Friday?", CreatedAt = DateTime.UtcNow, EndDate = DateTime.UtcNow.AddDays(5), CreatedBy = "creator", IsActive = true }
        );

        modelBuilder.Entity<SurveyOption>().HasData(
            new SurveyOption { Id = 1, SurveyId = 1, Text = "Pizza & Pasta", Votes = 5 },
            new SurveyOption { Id = 2, SurveyId = 1, Text = "Sushi & Asian Fusion", Votes = 8 },
            new SurveyOption { Id = 3, SurveyId = 1, Text = "Healthy Salads & Wraps", Votes = 3 }
        );

        modelBuilder.Entity<Album>().HasData(
            new Album { Id = 1, Title = "Company Outing 2023", Description = "Photos from our trip to Bali.", Type = "Image", CreatedAt = DateTime.UtcNow.AddMonths(-2) },
            new Album { Id = 2, Title = "Townhall Meetings", Description = "Recordings of our monthly townhall meetings.", Type = "Video", CreatedAt = DateTime.UtcNow.AddMonths(-1) }
        );

        modelBuilder.Entity<MediaItem>().HasData(
            new MediaItem { Id = 1, AlbumId = 1, Title = "Team on the Beach", Description = "Everyone having fun.", MediaUrl = "https://placehold.co/800x600/2563eb/ffffff?text=Beach+Photo", ThumbnailUrl = "https://placehold.co/300x200/2563eb/ffffff?text=Beach+Photo", Tags = "Outing, Bali, Team", Type = "Image", Likes = 15, CreatedAt = DateTime.UtcNow.AddMonths(-2) },
            new MediaItem { Id = 2, AlbumId = 2, Title = "Townhall Q3", Description = "Quarter 3 updates and financial results.", MediaUrl = "https://www.w3schools.com/html/mov_bbb.mp4", ThumbnailUrl = "https://placehold.co/300x200/1e293b/ffffff?text=Video+Thumbnail", Tags = "Townhall, Q3, Leadership", Type = "Video", Likes = 5, CreatedAt = DateTime.UtcNow.AddMonths(-1) }
        );

        modelBuilder.Entity<ChatMessage>().HasData(
            new ChatMessage { Id = 1, Sender = "user", Receiver = "admin", Content = "Hi Admin, could you help me with my laptop?", SentAt = DateTime.UtcNow.AddHours(-2) },
            new ChatMessage { Id = 2, Sender = "admin", Receiver = "user", Content = "Sure, what is the issue?", SentAt = DateTime.UtcNow.AddHours(-1) }
        );
    }
}