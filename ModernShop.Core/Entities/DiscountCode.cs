using ModernShop.Core.Enums;

namespace ModernShop.Core.Entities;

public class DiscountCode
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public DiscountType Type { get; set; }
    public decimal Amount { get; set; }              // درصد یا مبلغ ثابت
    public decimal? MinOrderAmount { get; set; }
    public int? MaxUsageCount { get; set; }
    public int UsedCount { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}
