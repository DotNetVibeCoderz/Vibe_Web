namespace LitePost.Data.Models
{
    public class PollVote
    {
        public int Id { get; set; }
        
        public int PollOptionId { get; set; }
        public PollOption PollOption { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;
    }
}