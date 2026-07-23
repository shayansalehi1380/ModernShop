using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Core.Enums;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

[ApiController]
[Route("api/orders")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly IPaymentGatewayService _paymentGateway;

    public OrdersController(AppDbContext db, ICurrentUserService currentUser, IPaymentGatewayService paymentGateway)
    {
        _db = db;
        _currentUser = currentUser;
        _paymentGateway = paymentGateway;
    }

    // مربوط به دکمه «ثبت نهایی سفارش» تو checkout.html
    [HttpPost]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
    {
        var userId = _currentUser.UserId!.Value;

        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId);
        if (address is null) return BadRequest(new { message = "آدرس معتبر نیست" });

        var cart = await _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart is null || cart.Items.Count == 0)
            return BadRequest(new { message = "سبد خرید شما خالی است" });

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        var settings = await _db.AppSettings
            .Where(s => s.Key == "DefaultShippingCost" || s.Key == "FreeShippingThreshold")
            .ToDictionaryAsync(s => s.Key, s => s.Value);
        var shippingCost = settings.TryGetValue("DefaultShippingCost", out var sc) ? decimal.Parse(sc) : 45000;
        var threshold = settings.TryGetValue("FreeShippingThreshold", out var th) ? decimal.Parse(th) : 5000000;
        var actualShipping = subtotal >= threshold ? 0 : shippingCost;

        decimal discountAmount = 0;
        DiscountCode? discountCode = null;

        if (!string.IsNullOrWhiteSpace(request.DiscountCode))
        {
            discountCode = await _db.DiscountCodes.FirstOrDefaultAsync(d => d.Code == request.DiscountCode && d.IsActive);
            if (discountCode is not null)
            {
                discountAmount = discountCode.Type == DiscountType.Percent
                    ? Math.Round(subtotal * discountCode.Amount / 100)
                    : discountCode.Amount;
                discountCode.UsedCount++;
            }
        }

        var order = new Order
        {
            OrderNumber = "ATL-" + Random.Shared.Next(100000, 999999),
            UserId = userId,
            AddressId = address.Id,

            // اسنپ‌شات آدرس در لحظه ثبت سفارش
            ShippingFullName = address.ReceiverFullName,
            ShippingPhone = address.ReceiverPhone,
            ShippingCity = address.City,
            ShippingFullAddress = address.FullAddress,
            ShippingPostalCode = address.PostalCode,

            DiscountCodeId = discountCode?.Id,
            PaymentMethod = request.PaymentMethod,
            Status = OrderStatus.PendingPayment,
            SubTotal = subtotal,
            ShippingCost = actualShipping,
            DiscountAmount = discountAmount,
            TotalAmount = subtotal + actualShipping - discountAmount
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                ProductId = item.ProductId,
                ProductNameSnapshot = item.Product.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.UnitPrice * item.Quantity
            });
        }

        order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.PendingPayment, Note = "سفارش ثبت شد" });

        _db.Orders.Add(order);
        _db.CartItems.RemoveRange(cart.Items);
        await _db.SaveChangesAsync();

        string? paymentUrl = null;

        if (request.PaymentMethod == PaymentMethod.Online)
        {
            paymentUrl = await _paymentGateway.RequestPaymentAsync(order.Id, order.TotalAmount);
            var authority = paymentUrl.Split('/').Last();

            _db.Payments.Add(new Payment
            {
                OrderId = order.Id,
                Amount = order.TotalAmount,
                GatewayName = "Zarinpal",
                TransactionCode = authority
            });
            await _db.SaveChangesAsync();
        }
        else
        {
            order.Status = OrderStatus.Processing;
            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Processing, Note = "سفارش پرداخت‌درمحل ثبت شد" });
            await _db.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetByNumber), new { orderNumber = order.OrderNumber },
            new { order = MapToDto(order), paymentUrl });
    }

    // مربوط به لیست «سفارش‌های من» تو account.html
    [HttpGet]
    public async Task<ActionResult<List<OrderListItemDto>>> GetMyOrders()
    {
        var userId = _currentUser.UserId!.Value;

        var orders = await _db.Orders
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new OrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Status = o.Status,
                ItemCount = o.Items.Sum(i => i.Quantity),
                TotalAmount = o.TotalAmount,
                ThumbnailImageUrl = o.Items
                    .Select(i => i.Product.Images.Where(im => im.IsMain).Select(im => im.ImageUrl).FirstOrDefault()
                                 ?? i.Product.Images.Select(im => im.ImageUrl).FirstOrDefault())
                    .FirstOrDefault() ?? "",
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(orders);
    }

    // مربوط به order-complete.html (تایم‌لاین پیگیری) و جزئیات سفارش تو account.html
    [HttpGet("{orderNumber}")]
    public async Task<ActionResult<OrderDto>> GetByNumber(string orderNumber)
    {
        var userId = _currentUser.UserId!.Value;

        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == userId);

        if (order is null) return NotFound();

        return Ok(MapToDto(order));
    }

    // مربوط به دکمه «تلاش مجدد پرداخت» تو order-complete.html (حالت ناموفق)
    [HttpPost("{orderNumber}/retry-payment")]
    public async Task<IActionResult> RetryPayment(string orderNumber)
    {
        var userId = _currentUser.UserId!.Value;
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && o.UserId == userId);
        if (order is null) return NotFound();

        if (order.Status is not (OrderStatus.Failed or OrderStatus.PendingPayment))
            return BadRequest(new { message = "این سفارش قابل تلاش مجدد نیست" });

        var paymentUrl = await _paymentGateway.RequestPaymentAsync(order.Id, order.TotalAmount);
        var authority = paymentUrl.Split('/').Last();

        _db.Payments.Add(new Payment
        {
            OrderId = order.Id,
            Amount = order.TotalAmount,
            GatewayName = "Zarinpal",
            TransactionCode = authority
        });
        await _db.SaveChangesAsync();

        return Ok(new { paymentUrl });
    }

    // آدرس callback زرین‌پال - همینو تو appsettings.json بخش Zarinpal:CallbackUrl بذار
    [HttpGet("payment-callback")]
    [AllowAnonymous]
    public async Task<IActionResult> PaymentCallback([FromQuery] int orderId, [FromQuery] string Authority, [FromQuery] string Status)
    {
        var order = await _db.Orders
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order is null) return NotFound();

        var payment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault(p => p.TransactionCode == Authority);
        if (payment is null) return NotFound();

        if (Status != "OK")
        {
            payment.Status = PaymentStatus.Failed;
            order.Status = OrderStatus.Failed;
            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Failed, Note = "پرداخت توسط کاربر لغو شد" });
            await _db.SaveChangesAsync();
            return Redirect($"/order-complete.html?status=failed&orderNumber={order.OrderNumber}");
        }

        var verified = await _paymentGateway.VerifyPaymentAsync(Authority, order.TotalAmount);

        if (verified)
        {
            payment.Status = PaymentStatus.Success;
            payment.PaidAt = DateTime.UtcNow;
            order.Status = OrderStatus.Processing;
            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Processing, Note = "پرداخت با موفقیت انجام شد" });
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            order.Status = OrderStatus.Failed;
            order.StatusHistory.Add(new OrderStatusHistory { Status = OrderStatus.Failed, Note = "تایید پرداخت ناموفق بود" });
        }

        await _db.SaveChangesAsync();

        var redirectStatus = verified ? "success" : "failed";
        return Redirect($"/order-complete.html?status={redirectStatus}&orderNumber={order.OrderNumber}");
    }

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        OrderNumber = order.OrderNumber,
        Status = order.Status,
        PaymentMethod = order.PaymentMethod,
        SubTotal = order.SubTotal,
        ShippingCost = order.ShippingCost,
        DiscountAmount = order.DiscountAmount,
        TotalAmount = order.TotalAmount,
        CreatedAt = order.CreatedAt,
        Items = order.Items.Select(i => new OrderItemDto
        {
            ProductName = i.ProductNameSnapshot,
            ImageUrl = i.Product?.Images?.FirstOrDefault(im => im.IsMain)?.ImageUrl
                       ?? i.Product?.Images?.FirstOrDefault()?.ImageUrl ?? "",
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            TotalPrice = i.TotalPrice
        }).ToList(),
        StatusHistory = order.StatusHistory
            .OrderBy(s => s.CreatedAt)
            .Select(s => new OrderStatusHistoryDto { Status = s.Status, Note = s.Note, CreatedAt = s.CreatedAt })
            .ToList()
    };
}
