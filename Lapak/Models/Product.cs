namespace Lapak.Models;

/// <summary>
/// Product entity - core of the e-commerce platform
/// </summary>
public class Product : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ShortDescription { get; set; }

    // Pricing
    public decimal Price { get; set; }
    public decimal? OriginalPrice { get; set; } // For showing discount
    public decimal? DiscountPercentage { get; set; }

    // Stock
    public int Stock { get; set; } = 0;
    public int MinOrder { get; set; } = 1;
    public int MaxOrder { get; set; } = 10;
    public string StockStatus { get; set; } = "Available"; // Available, LowStock, OutOfStock, PreOrder

    // Media
    public string? MainImageUrl { get; set; }
    public string? AdditionalImagesJson { get; set; } // JSON array of URLs

    // Metadata
    public string? TagsJson { get; set; } // JSON array of tag strings
    public string? AttributesJson { get; set; } // JSON for custom attributes
    public int ViewCount { get; set; } = 0;
    public int SoldCount { get; set; } = 0;
    public double AverageRating { get; set; } = 0;
    public int RatingCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; } = false;

    // Weight & Dimensions (for shipping)
    public decimal WeightInGrams { get; set; } = 0;
    public decimal? LengthCm { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }

    // Foreign Keys
    public Guid CategoryId { get; set; }
    public Guid StoreId { get; set; }

    // Navigation Properties
    public Category Category { get; set; } = null!;
    public Store Store { get; set; } = null!;
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<ProductLike> Likes { get; set; } = new List<ProductLike>();
    public ICollection<ProductPromo> Promos { get; set; } = new List<ProductPromo>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
