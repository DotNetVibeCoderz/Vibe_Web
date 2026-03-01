using System;
using System.ComponentModel.DataAnnotations;

namespace LMSNet.Data
{
    public class LearningDiscussion
    {
        public int Id { get; set; }
        
        [Required]
        public int LessonId { get; set; }
        public virtual Lesson Lesson { get; set; } = null!;
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        public virtual ApplicationUser User { get; set; } = null!;
        
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
