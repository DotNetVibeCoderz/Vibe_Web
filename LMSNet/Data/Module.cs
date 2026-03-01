using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LMSNet.Data
{
    public class Module
    {
        public int Id { get; set; }
        
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public int OrderIndex { get; set; }
        
        [Required]
        public int CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;
        
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    }
}
