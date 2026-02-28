namespace Intranet.Models;

public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "user"; // admin, user, creator
    public string AvatarUrl { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class NewsItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // HTML
    public string CoverImageUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Author { get; set; } = string.Empty;
    public string Category { get; set; } = "News"; 
}

public class Document
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Uploader { get; set; } = string.Empty;
}

public class SocialPost
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string AttachedImageUrl { get; set; } = string.Empty;
    public string AttachedFileUrl { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Author { get; set; } = string.Empty;
    public string AuthorAvatar { get; set; } = string.Empty;
    public int Likes { get; set; }
    public List<SocialComment> Comments { get; set; } = new();
}

public class SocialComment
{
    public int Id { get; set; }
    public int SocialPostId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorAvatar { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class SocialLike
{
    public int Id { get; set; }
    public int SocialPostId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class AppSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string Status { get; set; } = "Active"; 
}

public class TaskItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Assignee { get; set; } = string.Empty;
    public string Status { get; set; } = "Todo"; 
    public DateTime DueDate { get; set; }
}

public class AppItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string IconUrl { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class WikiArticle
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class FaqItem
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class ForumTopic
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Likes { get; set; }
    public List<ForumPost> Posts { get; set; } = new();
}

public class ForumPost
{
    public int Id { get; set; }
    public int ForumTopicId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Reward
{
    public int Id { get; set; }
    public string Receiver { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string BadgeIcon { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class Survey
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime EndDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<SurveyOption> Options { get; set; } = new();
}

public class SurveyOption
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Votes { get; set; }
}

public class UserVote
{
    public int Id { get; set; }
    public int SurveyId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class Album
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "Image"; 
    public DateTime CreatedAt { get; set; }
    public List<MediaItem> MediaItems { get; set; } = new();
}

public class MediaItem
{
    public int Id { get; set; }
    public int AlbumId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MediaUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string Type { get; set; } = "Image"; 
    public int Likes { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<MediaComment> Comments { get; set; } = new();
}

public class MediaComment
{
    public int Id { get; set; }
    public int MediaItemId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class MediaLike
{
    public int Id { get; set; }
    public int MediaItemId { get; set; }
    public string Username { get; set; } = string.Empty;
}

public class ChatMessage
{
    public int Id { get; set; }
    public string Sender { get; set; } = string.Empty;
    public string Receiver { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; } = false;
    public string? FileUrl { get; set; }
    public string? FileName { get; set; }
    public bool IsImage { get; set; } = false;
}