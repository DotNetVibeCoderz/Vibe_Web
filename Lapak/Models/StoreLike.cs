namespace Lapak.Models;

/// <summary>
/// Store like (favorite store)
/// </summary>
public class StoreLike : EntityBase
{
    public Guid UserId { get; set; }
    public Guid StoreId { get; set; }

    public User User { get; set; } = null!;
    public Store Store { get; set; } = null!;
}
