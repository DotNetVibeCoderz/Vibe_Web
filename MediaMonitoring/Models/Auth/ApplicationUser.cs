using System.ComponentModel.DataAnnotations;

namespace MediaMonitoring.Models.Auth
{
    /// <summary>
    /// Model user untuk sistem autentikasi
    /// </summary>
    public class ApplicationUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string Salt { get; set; } = string.Empty;

        public string Role { get; set; } = "User"; // Admin, Analyst, Viewer

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLogin { get; set; }

        public string? FullName { get; set; }

        public string? Organization { get; set; }
    }

    /// <summary>
    /// Audit trail untuk aktivitas pengguna
    /// </summary>
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string Action { get; set; } = string.Empty; // Login, View, Export, Configure, etc.

        public string EntityType { get; set; } = string.Empty; // Post, Alert, Settings, etc.

        public int? EntityId { get; set; }

        public string Details { get; set; } = string.Empty;

        public string IpAddress { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public string UserAgent { get; set; } = string.Empty;
    }

    /// <summary>
    /// Roles yang tersedia dalam sistem
    /// </summary>
    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Analyst = "Analyst";
        public const string Viewer = "Viewer";
    }
}