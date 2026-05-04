using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FormDezigner.Models
{
    public class FormVersion
    {
        [Key]
        public Guid Id { get; set; }
        public Guid FormEntityId { get; set; }
        [ForeignKey("FormEntityId")]
        public FormEntity? Form { get; set; }

        public int VersionNumber { get; set; }
        
        // Storing structure as JSON
        public string FormStructureJson { get; set; } = string.Empty;
        
        // Storing backend code
        public string BackendCode { get; set; } = string.Empty;

        public DateTime PublishedAt { get; set; }
        public bool IsActive { get; set; }
    }
}
