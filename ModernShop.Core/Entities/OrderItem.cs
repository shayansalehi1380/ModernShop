namespace ModernShop.Core.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }   // برای اینکه موقع ثبت سفارش موجودی همون تنوع (نه کل محصول) کم بشه
    public ProductVariant? ProductVariant { get; set; }

    public string ProductNameSnapshot { get; set; } = null!;  // حتی اگر اسم محصول بعدا عوض بشه
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
