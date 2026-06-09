using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SimpleBlog.Models
{
    public class BlogPost
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required(ErrorMessage = "Title is required, Bos!")]
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        
        // Remove Required here to prevent EditForm blocking before JS sync
        public string Content { get; set; } = string.Empty;
        
        public string? FeaturedImage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PublishedAt { get; set; }
        public bool IsPublished { get; set; }
        public int Likes { get; set; }
        public int Views { get; set; }

        public Guid CategoryId { get; set; }
        public Category? Category { get; set; }
        public ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
        public ICollection<Comment> Comments { get; set; } = new List<Comment>();
        public string AuthorId { get; set; } = string.Empty;
    }

    public class Category
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Icon { get; set; } = "📁";
        public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
    }

    public class Tag
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; } = string.Empty;
        public ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
    }

    public class BlogPostTag
    {
        public Guid BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; } = null!;
        public Guid TagId { get; set; }
        public Tag Tag { get; set; } = null!;
    }

    public class Comment
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsApproved { get; set; }
        public Guid BlogPostId { get; set; }
        public BlogPost BlogPost { get; set; } = null!;
    }

    public class ContactMessage
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class VisitorLog
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? Path { get; set; }
        public DateTime VisitedAt { get; set; } = DateTime.UtcNow;
    }
}
