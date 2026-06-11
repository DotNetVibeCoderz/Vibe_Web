// Model artikel kesehatan
namespace VirtualDoctor.Models;

public class HealthArticle
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Author { get; set; }
    public string? Category { get; set; }
    public string? ImageUrl { get; set; }
    public string? PdfUrl { get; set; }
    public string? SourceUrl { get; set; }
    public int ViewCount { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public bool IsIndexed { get; set; } // Sudah di-index ke vector DB?
    public DateTime? IndexedAt { get; set; }
}
