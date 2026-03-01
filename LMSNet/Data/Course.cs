using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMSNet.Data
{
    public class Course
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        // Use HTML Editor for this field
        public string Description { get; set; } = string.Empty;
        
        public string ThumbnailUrl { get; set; } = string.Empty;
        public decimal Price { get; set; }
        
        public string Status { get; set; } = "Draft"; // Draft, Published, Archived
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Navigation properties
        [Required]
        public string InstructorId { get; set; } = string.Empty;
        public virtual ApplicationUser Instructor { get; set; } = null!;
        
        public virtual ICollection<Module> Modules { get; set; } = new List<Module>();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}
