namespace LitePost.Data.Models
{
    public class UserListMember
    {
        public int ListId { get; set; }
        public UserList UserList { get; set; } = null!;

        public int UserId { get; set; }
        public User User { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }
}