using ModernShop.Core.Enums;

namespace ModernShop.Core.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
// همین جدول برای نمایش تایم‌لاین پیگیری سفارش استفاده می‌شه
