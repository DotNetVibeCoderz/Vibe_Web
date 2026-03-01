using System.ComponentModel.DataAnnotations;

namespace LMSNet.Data
{
    public class Lesson
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        // This will be rich text Content
        public string Content { get; set; } = string.Empty;
        
        public string VideoUrl { get; set; } = string.Empty;
        
        public string AttachmentUrl { get; set; } = string.Empty;
        
        public int OrderIndex { get; set; }
        
        public string Type { get; set; } = "Video"; // Video, Article, Quiz
        
        [Required]
        public int ModuleId { get; set; }
        public virtual Module Module { get; set; } = null!;
    }
}
