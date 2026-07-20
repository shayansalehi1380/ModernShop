namespace ModernShop.Core.Interfaces;

/// <summary>
/// ارسال پیامک OTP. پیاده‌سازی واقعی (اتصال به سرویس پیامکی) در Atelier.Infrastructure قرار می‌گیره.
/// </summary>
public interface ISmsService
{
    Task SendOtpAsync(string phoneNumber, string code);
}

/// <summary>
/// اتصال به درگاه پرداخت (مثل زرین‌پال). پیاده‌سازی واقعی در Atelier.Infrastructure قرار می‌گیره.
/// </summary>
public interface IPaymentGatewayService
{
    /// <returns>لینک انتقال کاربر به درگاه پرداخت</returns>
    Task<string> RequestPaymentAsync(int orderId, decimal amount);

    Task<bool> VerifyPaymentAsync(string transactionCode, decimal amount);
}

/// <summary>
/// دسترسی به اطلاعات کاربر لاگین‌کرده‌ی فعلی (بر اساس JWT). پیاده‌سازی در لایه Api قرار می‌گیره.
/// </summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? PhoneNumber { get; }
    bool IsAuthenticated { get; }
}
