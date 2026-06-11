// Model obat
namespace VirtualDoctor.Models;

public class Medicine
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty; // Obat Bebas, Obat Keras, Vitamin, Suplemen
    public string? Description { get; set; }
    public string? Dosage { get; set; }
    public string? SideEffects { get; set; }
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool RequiresPrescription { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public double Rating { get; set; } = 4.0;
    public int TotalSold { get; set; }
}
