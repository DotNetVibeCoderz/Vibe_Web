namespace Lapak.Models;

/// <summary>
/// Store review with rating
/// </summary>
public class StoreReview : EntityBase
{
    public string? Comment { get; set; }
    public int Rating { get; set; } = 0; // 1-5 stars

    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }

    public User User { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
