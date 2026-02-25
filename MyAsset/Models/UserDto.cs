using System.ComponentModel.DataAnnotations;

namespace MyAsset.Models
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;

        // Password is required only for new users
        public string Password { get; set; } = string.Empty;

        public List<string> Roles { get; set; } = new();
    }
}