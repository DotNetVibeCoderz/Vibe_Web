namespace LitePost.Data.Models
{
    public enum NotificationType
    {
        Like,
        Reply,
        Follow,
        Mention
    }

    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Receiver
        public User User { get; set; } = null!;

        public int ActorId { get; set; } // Triggerer
        public User Actor { get; set; } = null!;

        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public int? RelatedPostId { get; set; }

        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}