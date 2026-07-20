using System.Net.Http.Json;
using ModernShop.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace ModernShop.Infrastructure.Services;

public class ZarinpalSettings
{
    public string MerchantId { get; set; } = null!;
    public string CallbackUrl { get; set; } = null!;
    public bool IsSandbox { get; set; } = true;
}

/// <summary>
/// پیاده‌سازی درگاه پرداخت زرین‌پال (API نسخه ۴). اگه از درگاه دیگه‌ای استفاده می‌کنی
/// (آیدی‌پی، به‌پرداخت ملت و ...)، فقط همین کلاس رو با یک پیاده‌سازی جدید از IPaymentGatewayService
/// جایگزین کن؛ بقیه پروژه به جزئیات درگاه وابسته نیست.
/// </summary>
public class ZarinpalPaymentService : IPaymentGatewayService
{
    private readonly HttpClient _httpClient;
    private readonly ZarinpalSettings _settings;

    public ZarinpalPaymentService(HttpClient httpClient, IOptions<ZarinpalSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    private string BaseUrl => _settings.IsSandbox
        ? "https://sandbox.zarinpal.com/pg/v4/payment"
        : "https://payment.zarinpal.com/pg/v4/payment";

    private string GatewayStartUrl => _settings.IsSandbox
        ? "https://sandbox.zarinpal.com/pg/StartPay"
        : "https://payment.zarinpal.com/pg/StartPay";

    public async Task<string> RequestPaymentAsync(int orderId, decimal amount)
    {
        var requestBody = new
        {
            merchant_id = _settings.MerchantId,
            amount = (long)amount * 10, // زرین‌پال مبلغ رو به ریال می‌گیره، تومان ضربدر ۱۰
            callback_url = $"{_settings.CallbackUrl}?orderId={orderId}",
            description = $"پرداخت سفارش شماره {orderId}"
        };

        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/request.json", requestBody);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ZarinpalRequestResponse>();
        var authority = result?.Data?.Authority
            ?? throw new InvalidOperationException("درگاه پرداخت زرین‌پال پاسخ معتبری برنگردوند");

        // این authority همون چیزیه که باید تو Payment.TransactionCode ذخیره بشه
        return $"{GatewayStartUrl}/{authority}";
    }

    public async Task<bool> VerifyPaymentAsync(string transactionCode, decimal amount)
    {
        var requestBody = new
        {
            merchant_id = _settings.MerchantId,
            amount = (long)amount * 10,
            authority = transactionCode
        };

        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/verify.json", requestBody);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ZarinpalVerifyResponse>();

        // کد ۱۰۰ یعنی همین الان با موفقیت تایید شد، ۱۰۱ یعنی قبلاً تایید شده بود
        return result?.Data?.Code is 100 or 101;
    }

    private class ZarinpalRequestResponse
    {
        public ZarinpalRequestData? Data { get; set; }
    }

    private class ZarinpalRequestData
    {
        public string? Authority { get; set; }
        public int Code { get; set; }
    }

    private class ZarinpalVerifyResponse
    {
        public ZarinpalVerifyData? Data { get; set; }
    }

    private class ZarinpalVerifyData
    {
        public int Code { get; set; }
        public string? RefId { get; set; }
    }
}
