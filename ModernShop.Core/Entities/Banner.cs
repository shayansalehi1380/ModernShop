namespace ModernShop.Core.Entities;

public class Banner
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = null!;
    public string? Title { get; set; }
    public string? LinkUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
