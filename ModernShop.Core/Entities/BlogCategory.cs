namespace ModernShop.Core.Entities;

public class BlogCategory
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;

    public ICollection<BlogPost> Posts { get; set; } = new List<BlogPost>();
}
