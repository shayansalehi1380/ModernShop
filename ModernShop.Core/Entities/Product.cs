namespace ModernShop.Core.Entities;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Sku { get; set; }
    public string? Description { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int? BrandId { get; set; }
    public Brand? Brand { get; set; }

    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    // اگه فعال باشه، صفحه محصول همیشه «فقط ۱ عدد در انبار مانده» رو نشون می‌ده (صرف‌نظر از
    // موجودی واقعی) تا حس فوریت خرید رو تحریک کنه؛ اگه غیرفعال باشه، این پیام فقط وقتی نشون
    // داده می‌شه که موجودی واقعی دقیقاً ۱ باشه
    public bool ForceLowStockBadge { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductSpecification> Specifications { get; set; } = new List<ProductSpecification>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<WishlistItem> WishlistedBy { get; set; } = new List<WishlistItem>();
}
