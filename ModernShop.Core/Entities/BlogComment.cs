namespace ModernShop.Core.Entities;

public class BlogComment
{
    public int Id { get; set; }
    public int BlogPostId { get; set; }
    public BlogPost BlogPost { get; set; } = null!;
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public string Content { get; set; } = null!;
    public bool IsApproved { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
