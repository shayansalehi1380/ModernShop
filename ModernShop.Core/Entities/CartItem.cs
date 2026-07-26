namespace ModernShop.Core.Entities;

public class CartItem
{
    public int Id { get; set; }
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }   // قیمت لحظه افزودن (اسنپ‌شات)

    // رزرو موجودی: تا این لحظه این تعداد از موجودی محصول برای همین سبد کنار گذاشته شده و کاربرهای
    // دیگه نمی‌تونن همون موجودی رو بخرن؛ هر بار افزودن/تغییر تعداد این مقدار ۲ ساعت جلو کشیده می‌شه.
    // اگه کاربر تا این لحظه خرید رو نهایی نکنه، CartReservationCleanupService این ردیف رو پاک
    // می‌کنه و موجودی برای بقیه آزاد می‌شه.
    public DateTime ReservedUntil { get; set; } = DateTime.UtcNow.AddHours(2);
}
