namespace ModernShop.Core.Entities;


public class AppSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = null!;      // مثلا "FreeShippingThreshold"
    public string Value { get; set; } = null!;     // مثلا "5000000"
    public string? Description { get; set; }
}
// نمونه ردیف‌ها:
// FreeShippingThreshold = 5000000
// DefaultShippingCost   = 45000
// OtpExpiryMinutes      = 2
// OtpCodeLength         = 5
