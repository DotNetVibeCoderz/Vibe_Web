namespace Lapak.Models;

/// <summary>
/// Product like (heart/favorite)
/// </summary>
public class ProductLike : EntityBase
{
    public Guid UserId { get; set; }
    public Guid ProductId { get; set; }

    public User User { get; set; } = null!;
    public Product Product { get; set; } = null!;
}
