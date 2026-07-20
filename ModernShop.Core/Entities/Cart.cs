namespace ModernShop.Core.Entities;

public class Cart
{
    public int Id { get; set; }
    public int? UserId { get; set; }            // کاربر لاگین‌کرده
    public User? User { get; set; }
    public string? GuestSessionId { get; set; } // برای کاربر مهمان (قبل از لاگین)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}
