namespace ModernShop.Core.Entities;

// صفحات ثابت فوتر: داستان ما، تماس با ما، سوالات متداول، شرایط بازگشت کالا
// اسلاگ‌ها ثابت و از پیش تعریف‌شده‌ان (فقط عنوان/متن از پنل مدیریت قابل ویرایشه، نه افزودن/حذف صفحه)
public class StaticPage
{
    public int Id { get; set; }
    public string Slug { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Content { get; set; } = null!;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
