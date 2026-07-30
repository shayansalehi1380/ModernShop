using ModernShop.Core.Interfaces;
using Microsoft.Extensions.Options;
using System.Text;
using System.Xml.Linq;

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
    // نام کاربری و رمز عبور (یا ApiKey، طبق پنل ملی‌پیامک) - این مقادیر رو مستقیم روی appsettings.json
    // سمت سرور پر کنید، نه اینکه از طریق چت برای کسی ارسالشون کنید.
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    // آدرس وبسرویس SOAP - این متد (SendByBaseNumber2) با الگوهایی کار می‌کنه که از داخل پنل
    // ملی‌پیامک، بخش «ابزار ویژه > خط خدماتی اشتراکی» ثبت و تایید شده باشن (نه بخش «توسعه‌دهندگان»).
    // از HTTPS استفاده می‌کنیم (نه HTTP خام مستندات) چون خیلی از هاست‌ها/فایروال‌ها خروجی HTTP
    // ساده (پورت ۸۰) رو مسدود می‌کنن ولی HTTPS (۴۴۳) باز می‌ذارن.
    public string ApiUrl { get; set; } = "https://api.payamak-panel.com/post/Send.asmx";

    public MeliPayamakBodyIds BodyIds { get; set; } = new();
}

/// <summary>
/// پیاده‌سازی وبسرویس SOAP ملی‌پیامک، متد SendByBaseNumber2، برای ارسال پیامک با متن پیشفرض از
/// «خط خدماتی اشتراکی» (یعنی متن پیامک از قبل تو پنل ملی‌پیامک - بخش ابزار ویژه - تعریف و تایید
/// شده و اینجا فقط bodyId + متغیرهای الگو پاس داده می‌شن، نه متن آزاد).
/// مستندات: http://api.payamak-panel.com/post/Send.asmx?wsdl (namespace: http://tempuri.org/)
/// </summary>
public class MeliPayamakSmsService : IOrderNotificationSmsService
{
    private static readonly XNamespace Tns = "http://tempuri.org/";
    private static readonly XNamespace Soap = "http://schemas.xmlsoap.org/soap/envelope/";

    private readonly HttpClient _httpClient;
    private readonly MeliPayamakSettings _settings;

    public MeliPayamakSmsService(HttpClient httpClient, IOptions<MeliPayamakSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task SendOrderStatusAsync(string phoneNumber, string bodyId, params string[] variables)
    {
        var text = string.Join(";", variables);

        // نکته‌ی مهم: schema این وبسرویس elementFormDefault="qualified" هست، یعنی
        // پارامترهای داخلی (username/password/text/to/bodyId) هم باید داخل namespace
        // tempuri.org باشن، نه فقط خودِ عنصر SendByBaseNumber2 - وگرنه سریالایزر مجبور
        // می‌شه رو هر کدوم xmlns="" بذاره و سرور اونا رو null/خالی می‌بینه (باعث خطای
        // «نام کاربری یا رمز عبور اشتباه است» می‌شه چون اصلاً هیچ مقداری بهش نمی‌رسه).
        var envelope = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(Soap + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soap", Soap.NamespaceName),
                new XElement(Soap + "Body",
                    new XElement(Tns + "SendByBaseNumber2",
                        new XElement(Tns + "username", _settings.Username),
                        new XElement(Tns + "password", _settings.Password),
                        new XElement(Tns + "text", text),
                        new XElement(Tns + "to", phoneNumber),
                        new XElement(Tns + "bodyId", bodyId)))));

        using var request = new HttpRequestMessage(HttpMethod.Post, _settings.ApiUrl)
        {
            Content = new StringContent(envelope.ToString(), Encoding.UTF8, "text/xml")
        };
        request.Headers.Add("SOAPAction", "\"http://tempuri.org/SendByBaseNumber2\"");

        using var response = await _httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException("ارسال پیامک وضعیت سفارش با خطا مواجه شد");
        }

        string? result;
        try
        {
            var responseDoc = XDocument.Parse(body);
            result = responseDoc.Descendants(Tns + "SendByBaseNumber2Result").FirstOrDefault()?.Value;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("پاسخ نامعتبر از سرویس پیامک ملی‌پیامک دریافت شد");
        }

        // موفق یعنی یک عدد بیش از ۱۵ رقم (شناسه پیامک)؛ هر مقدار دیگه (کد خطای منفی و کوتاه) یعنی ناموفق
        var isSuccess = !string.IsNullOrWhiteSpace(result) && result.Length > 15 && result.All(char.IsDigit);
        if (!isSuccess)
        {
            throw new InvalidOperationException($"ارسال پیامک وضعیت سفارش ناموفق بود (کد بازگشتی: {result ?? "نامشخص"})");
        }
    }
}
