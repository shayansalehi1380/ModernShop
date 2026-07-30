using ModernShop.Core.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ModernShop.Infrastructure.Services;

public class MeliPayamakBodyIds
{
    // کد متن (bodyId) الگوی «سفارش {0} با موفقیت ثبت شد ...» - ارسال به مشتری بعد از ثبت موفق سفارش
    public string OrderPlaced { get; set; } = "56056";

    // کد متن الگوی «سفارش {0} ارسال شد ...» - ارسال به مشتری وقتی ادمین وضعیت رو «در حال ارسال» می‌کنه
    public string Shipped { get; set; } = "56058";

    // کد متن الگوی «سفارش {0} با موفقیت تحویل داده شد ...» - ارسال به مشتری وقتی ادمین وضعیت رو «تحویل شده» می‌کنه
    public string Delivered { get; set; } = "56060";

    // کد متن الگوی «مدیر عزیز یک سفارش شماره {0} با موفقیت ثبت شد ...» - ارسال به ادمین بعد از ثبت موفق هر سفارش
    public string AdminNewOrder { get; set; } = "56365";
}

public class MeliPayamakSettings
{
    // نام کاربری و رمز عبور پنل ملی‌پیامک شما - این مقادیر رو مستقیم روی appsettings.json سمت سرور
    // پر کنید، نه اینکه از طریق چت برای کسی ارسالشون کنید.
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public string ApiUrl { get; set; } = "https://rest.payamak-panel.com/api/SendSMS/BaseServiceNumber";

    public MeliPayamakBodyIds BodyIds { get; set; } = new();
}

/// <summary>
/// پیاده‌سازی وبسرویس Rest ملی‌پیامک برای ارسال پیامک با متن پیشفرض از «خط خدماتی اشتراکی»
/// (یعنی متن پیامک از قبل تو پنل ملی‌پیامک تعریف و تایید شده و اینجا فقط bodyId + متغیرهای الگو
/// پاس داده می‌شن، نه متن آزاد). مستندات: https://rest.payamak-panel.com/api/SendSMS/BaseServiceNumber
/// </summary>
public class MeliPayamakSmsService : IOrderNotificationSmsService
{
    private readonly HttpClient _httpClient;
    private readonly MeliPayamakSettings _settings;

    public MeliPayamakSmsService(HttpClient httpClient, IOptions<MeliPayamakSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendOrderStatusAsync(string phoneNumber, string bodyId, params string[] variables)
    {
        var payload = new Dictionary<string, string>
        {
            ["username"] = _settings.Username,
            ["password"] = _settings.Password,
            ["to"] = phoneNumber,
            ["bodyId"] = bodyId,
            ["text"] = string.Join(";", variables)
        };

        using var response = await _httpClient.PostAsync(_settings.ApiUrl, new FormUrlEncodedContent(payload));
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("ارسال پیامک وضعیت سفارش با خطا مواجه شد");
        }

        // پاسخ موفق نمونه: {"Value":"recID","RetStatus":1,"StrRetStatus":"Ok"}
        // در صورت خطا: RetStatus برابر 35 و StrRetStatus برابر "InvalidData" و کد خطا داخل Value برمی‌گرده
        try
        {
            using var doc = JsonDocument.Parse(body);
            var retStatus = doc.RootElement.TryGetProperty("RetStatus", out var rs) ? rs.GetInt32() : 0;
            if (retStatus != 1)
            {
                var value = doc.RootElement.TryGetProperty("Value", out var v) ? v.ToString() : "نامشخص";
                throw new InvalidOperationException($"ارسال پیامک وضعیت سفارش ناموفق بود (کد خطا: {value})");
            }
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("پاسخ نامعتبر از سرویس پیامک ملی‌پیامک دریافت شد");
        }
    }
}
