namespace ModernShop.Api.Services;

/// <summary>
/// تنظیمات لازم برای اتصال به TorobAPI v3 - از appsettings.json (بخش Torob) خونده می‌شه.
/// </summary>
public class TorobSettings
{
    public string PublicKeyPem { get; set; } = null!;

    // مقدار claim به‌نام aud تو توکن ترب باید دقیقاً با همین مقدار برابر باشه (همون دامنه‌ای
    // که موقع ثبت آدرس API تو پنل ترب وارد کردید، بدون https:// و بدون اسلش انتهایی)
    public string ExpectedAudience { get; set; } = null!;

    // برای ساختن لینک مطلق محصولات و تصاویر (چون آدرس‌های ذخیره‌شده تو دیتابیس نسبی هستن)
    public string SiteBaseUrl { get; set; } = null!;
}
