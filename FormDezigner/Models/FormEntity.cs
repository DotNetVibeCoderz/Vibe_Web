using System.ComponentModel.DataAnnotations;

namespace FormDezigner.Models
{
    public class FormEntity
    {
        [Key]
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string TargetFramework { get; set; } = "MudBlazor"; // MudBlazor, Bootstrap, Tailwind
        public string BackendLanguage { get; set; } = "CSharp"; // CSharp, JS, Python
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<FormVersion> Versions { get; set; } = new List<FormVersion>();
    }
}
