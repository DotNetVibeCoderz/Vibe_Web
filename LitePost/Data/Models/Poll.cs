namespace LitePost.Data.Models
{
    public class Poll
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public Post Post { get; set; } = null!;

        public string Question { get; set; } = string.Empty;
        public DateTime EndsAt { get; set; }

        public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    }
}