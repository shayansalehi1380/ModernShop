namespace ModernShop.Core.Entities;

public class Brand
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? LogoUrl { get; set; }
    public bool IsFeatured { get; set; }     // نمایش تو نوار برندهای صفحه اصلی
    public int DisplayOrder { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
