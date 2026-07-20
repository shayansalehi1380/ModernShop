using ModernShop.Core.Enums;

namespace ModernShop.Core.Entities;

public class Payment
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;   // یک سفارش می‌تونه چند تلاش پرداخت داشته باشه (۱-به-چند)

    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? GatewayName { get; set; }      // مثلا زرین‌پال
    public string? TransactionCode { get; set; }
    public DateTime? PaidAt { get; set; }
}
