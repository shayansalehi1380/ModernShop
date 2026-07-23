namespace ModernShop.Core.Entities;

public class ProductReview
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int Rating { get; set; }   // ۱ تا ۵
    public string? Comment { get; set; }
    public bool IsApproved { get; set; }
    public string? AdminReply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
