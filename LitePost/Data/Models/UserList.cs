using System.ComponentModel.DataAnnotations;

namespace LitePost.Data.Models
{
    public class UserList
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Description { get; set; }

        public int OwnerId { get; set; }
        public User Owner { get; set; } = null!;

        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<UserListMember> Members { get; set; } = new List<UserListMember>();
    }
}