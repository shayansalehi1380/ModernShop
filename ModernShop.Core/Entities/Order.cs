using ModernShop.Core.Enums;

namespace ModernShop.Core.Entities;

public class Order
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = null!;   // مثل ATL-482913
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int AddressId { get; set; }                 // آدرس فعلی کاربر (ارجاع)
    public Address Address { get; set; } = null!;

    // اسنپ‌شات آدرس گیرنده در لحظه ثبت سفارش — چون اگه کاربر بعدا آدرسش رو
    // ویرایش یا حذف کنه، نباید اطلاعات سفارش‌های قبلی تغییر کنه
    public string ShippingFullName { get; set; } = null!;
    public string ShippingPhone { get; set; } = null!;
    public string ShippingCity { get; set; } = null!;
    public string ShippingFullAddress { get; set; } = null!;
    public string ShippingPostalCode { get; set; } = null!;

    public int? DiscountCodeId { get; set; }
    public DiscountCode? DiscountCode { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;
    public PaymentMethod PaymentMethod { get; set; }

    public decimal SubTotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
