namespace ModernShop.Api.Services;

/// <summary>
/// اطلاعات ورود پنل مدیریت (admin.html). چون فقط یک ادمین داریم، به جای جدول جدا تو دیتابیس،
/// از یک کاربر ثابت تو appsettings.json (بخش Admin) استفاده می‌کنیم.
/// </summary>
public class AdminSettings
{
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
}
