using ModernShop.Core.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ModernShop.Infrastructure.Services;

public class SmsSettings
{
    public string ApiKey { get; set; } = null!;

    // اسم الگویی که تو پنل کاوه‌نگار (بخش Verify Lookup) از قبل ساختی، مثلاً چیزی شبیه:
    //   کد تایید عضویت شما: %token%
    // این اسم رو باید از پنل کاوه‌نگار بگیری، نه چیزی که خودت اختراع کنی
    public string TemplateName { get; set; } = "SendOTPLogin";

    public string ApiBaseUrl { get; set; } = "https://api.kavenegar.com";

    // شماره خط ارسال‌کننده برای پیامک‌های اطلاع‌رسانی معمولی (SendAsync)، نه پیامک OTP.
    // پیش‌فرض همون خط آزمایشی کاوه‌نگار (فقط برای تست، حتما قبل از production با خط واقعی خودت عوضش کن).
    public string Sender { get; set; } = "2000660110";
}

/// <summary>
/// پیاده‌سازی متد Verify Lookup کاوه‌نگار (مخصوص ارسال کد تایید).
/// این متد نسبت به ارسال پیامک معمولی اولویت بالاتری داره و فیلتر نمی‌شه، ولی نیاز به
/// تعریف الگو (Template) از پنل کاوه‌نگار داره - قبل از استفاده حتماً یه الگو با پارامتر
/// %token% بساز و اسمش رو تو appsettings.json بخش Sms:TemplateName بذار.
/// مستندات: https://kavenegar.com/rest.html (بخش Lookup)
/// </summary>
public class SmsService : ISmsService
{
    private readonly HttpClient _httpClient;
    private readonly SmsSettings _settings;

    public SmsService(HttpClient httpClient, IOptions<SmsSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendOtpAsync(string phoneNumber, string code)
    {
        var receptor = Uri.EscapeDataString(phoneNumber);
        var token = Uri.EscapeDataString(code);
        var template = Uri.EscapeDataString(_settings.TemplateName);

        var url = $"{_settings.ApiBaseUrl}/v1/{_settings.ApiKey}/verify/lookup.json" +
                  $"?receptor={receptor}&token={token}&template={template}";

        var response = await _httpClient.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // کاوه‌نگار برای خطاهایی مثل اعتبار ناکافی (418)، الگوی نامعتبر (424) و ... کد HTTP غیر ۲xx برمی‌گردونه
            var message = TryExtractKavenegarMessage(body) ?? "ارسال پیامک کد تایید با خطا مواجه شد";
            throw new InvalidOperationException(message);
        }
    }

    // مربوط به اطلاع‌رسانی تغییر وضعیت سفارش تو پنل مدیریت (مثلا «سفارش ارسال شد»)
    public async Task SendAsync(string phoneNumber, string message)
    {
        var receptor = Uri.EscapeDataString(phoneNumber);
        var text = Uri.EscapeDataString(message);
        var sender = Uri.EscapeDataString(_settings.Sender);

        var url = $"{_settings.ApiBaseUrl}/v1/{_settings.ApiKey}/sms/send.json" +
                  $"?receptor={receptor}&message={text}&sender={sender}";

        var response = await _httpClient.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = TryExtractKavenegarMessage(body) ?? "ارسال پیامک با خطا مواجه شد";
            throw new InvalidOperationException(errorMessage);
        }
    }

    private static string? TryExtractKavenegarMessage(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("return", out var ret) &&
                ret.TryGetProperty("message", out var msg))
            {
                return msg.GetString();
            }
        }
        catch
        {
            // بدنه پاسخ JSON معتبر نبود؛ از پیام پیش‌فرض استفاده می‌شه
        }
        return null;
    }
}
