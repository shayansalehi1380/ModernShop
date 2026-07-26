using ModernShop.Core.Enums;

namespace ModernShop.Core.Entities;

public class Banner
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = null!;   // نسخه‌ی دسکتاپ
    public string? MobileImageUrl { get; set; }      // نسخه‌ی موبایل - خالی یعنی همون ImageUrl دسکتاپ استفاده بشه
    public BannerType Type { get; set; } = BannerType.Hero;
    public string? Title { get; set; }
    public string? LinkUrl { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }
}
