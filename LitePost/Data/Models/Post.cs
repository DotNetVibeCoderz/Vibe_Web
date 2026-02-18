using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LitePost.Data.Models
{
    public enum PostType
    {
        Tweet,
        Reply,
        Quote,
        Repost, // Added
        Article,
        LongVideo
    }

    public class Post
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; } = null!;

        [Required]
        [StringLength(2000)]
        public string Content { get; set; } = string.Empty;

        public string? MediaUrl { get; set; }
        public string? Location { get; set; } // Added
        public DateTime? ScheduledFor { get; set; } // Added

        public PostType Type { get; set; }

        public int? ParentPostId { get; set; }
        public Post? ParentPost { get; set; }

        public int? QuotePostId { get; set; }
        public Post? QuotePost { get; set; }
        
        // Poll
        public int? PollId { get; set; }
        public Poll? Poll { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }

        public ICollection<Like> Likes { get; set; } = new List<Like>();
        public ICollection<Post> Replies { get; set; } = new List<Post>();
        
        [NotMapped]
        public int LikeCount => Likes?.Count ?? 0;
        
        [NotMapped]
        public int ReplyCount => Replies?.Count ?? 0;
    }
}