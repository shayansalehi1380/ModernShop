namespace ModernShop.Core.Entities;

public class OtpCode
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = null!;
    public string Code { get; set; } = null!;           // ۵ رقمی
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
