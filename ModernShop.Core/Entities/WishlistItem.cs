namespace ModernShop.Core.Entities;

public class WishlistItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
// نکته: تو Infrastructure یک ایندکس یکتا روی (UserId, ProductId) بذار تا یک محصول دوبار اضافه نشه
