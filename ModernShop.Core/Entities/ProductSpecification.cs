namespace ModernShop.Core.Entities;

public class ProductSpecification
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Key { get; set; } = null!;      // مثلا "نوع اتصال"
    public string Value { get; set; } = null!;     // مثلا "بلوتوث ۵.۳"
    public int DisplayOrder { get; set; }
}
