namespace ModernShop.Core.Entities;

public class NewsletterSubscriber
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
}
