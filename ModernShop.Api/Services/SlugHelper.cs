using System.Text.RegularExpressions;

namespace ModernShop.Api.Services;

/// <summary>
/// تولید Slug از روی متن فارسی/انگلیسی، برای فرم‌های پنل مدیریت (محصول و پست وبلاگ)
/// که کاربر می‌تونه نامک رو خالی بذاره تا خودکار از روی عنوان ساخته بشه.
/// </summary>
public static class SlugHelper
{
    public static string Slugify(string input)
    {
        var lower = input.Trim().ToLowerInvariant();
        var dashed = Regex.Replace(lower, @"[\s_]+", "-");
        var cleaned = Regex.Replace(dashed, "[^a-z0-9؀-ۿ\\-]", "");
        return Regex.Replace(cleaned, "-+", "-").Trim('-');
    }
}
