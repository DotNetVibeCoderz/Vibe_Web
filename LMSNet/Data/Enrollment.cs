using System;
using System.ComponentModel.DataAnnotations;

namespace LMSNet.Data
{
    public class Enrollment
    {
        public int Id { get; set; }
        
        [Required]
        public string StudentId { get; set; } = string.Empty;
        public virtual ApplicationUser Student { get; set; } = null!;
        
        [Required]
        public int CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;
        
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public decimal ProgressPercentage { get; set; } = 0; // 0-100%
        
        public DateTime? CompletedAt { get; set; }
        public string CertificateUrl { get; set; } = string.Empty;

        // Added Rating field
        [Range(1, 5)]
        public int? Rating { get; set; }
        public string? Review { get; set; }
        public DateTime? RatedAt { get; set; }
    }
}
