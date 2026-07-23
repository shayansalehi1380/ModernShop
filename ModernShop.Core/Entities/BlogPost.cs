namespace ModernShop.Core.Entities;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = null!;
    public string? FeaturedImageUrl { get; set; }

    public int BlogCategoryId { get; set; }
    public BlogCategory BlogCategory { get; set; } = null!;
    public int AuthorId { get; set; }             // FK به User (نویسنده هم یک کاربره)
    public User Author { get; set; } = null!;
    public string? AuthorName { get; set; }        // نام دلخواه ادمین برای نمایش؛ خالی = نام کاربر سیستمی نویسنده

    public int ReadTimeMinutes { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public int ViewCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();
}
