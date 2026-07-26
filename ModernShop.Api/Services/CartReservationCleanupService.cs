using Microsoft.EntityFrameworkCore;
using ModernShop.Infrastructure.Data;

namespace ModernShop.Api.Services;

/// <summary>
/// وقتی کاربری محصولی رو به سبد اضافه می‌کنه، اون تعداد تا ۲ ساعت (CartItem.ReservedUntil) از
/// موجودی برای بقیه‌ی کاربرها رزرو می‌مونه (ببین CartController.GetAvailableStockAsync). این
/// سرویس پس‌زمینه هر چند دقیقه یک‌بار سبدهایی که این بازه‌شون تموم شده و کاربر هنوز خرید رو
/// نهایی نکرده رو پاک می‌کنه تا هم سبد کاربر واقعی (نه یه رزرو یتیم) رو نشون بده، هم جدول
/// CartItems بی‌دلیل بزرگ نشه. توجه: خودِ صحتِ «رزرو منقضی دیگه حساب نمی‌شه» به این سرویس
/// وابسته نیست (چون همه‌جا فیلتر ReservedUntil > الان زده می‌شه)؛ این فقط برای نظافت و تجربه
/// کاربریه.
/// </summary>
public class CartReservationCleanupService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    private readonly IServiceProvider _services;
    private readonly ILogger<CartReservationCleanupService> _logger;

    public CartReservationCleanupService(IServiceProvider services, ILogger<CartReservationCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;
                var removed = await db.CartItems
                    .Where(ci => ci.ReservedUntil < now)
                    .ExecuteDeleteAsync(stoppingToken);

                if (removed > 0)
                    _logger.LogInformation("Released {Count} expired cart reservation(s)", removed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cart reservation cleanup failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // برنامه در حال خاموش شدنه - طبیعیه
            }
        }
    }
}
