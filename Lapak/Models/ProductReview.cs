namespace Lapak.Models;

/// <summary>
/// Product review with rating and comment
/// </summary>
public class ProductReview : EntityBase
{
    public string? Comment { get; set; }
    public int Rating { get; set; } = 0; // 1-5 stars
    public string? ImageUrlsJson { get; set; }

    // Foreign Keys
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
