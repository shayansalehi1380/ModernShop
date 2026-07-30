using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Core.Enums;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using ModernShop.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «سفارشات» تو پنل مدیریت
[ApiController]
[Route("api/admin/orders")]
[Authorize(Policy = "AdminOnly")]
public class AdminOrdersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IOrderNotificationSmsService _smsService;
    private readonly MeliPayamakBodyIds _bodyIds;

    public AdminOrdersController(AppDbContext db, IOrderNotificationSmsService smsService, IOptions<MeliPayamakSettings> meliPayamakSettings)
    {
        _db = db;
        _smsService = smsService;
        _bodyIds = meliPayamakSettings.Value.BodyIds;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminOrderListItemDto>>> GetAll([FromQuery] OrderStatus? status)
    {
        var query = _db.Orders.Include(o => o.User).AsQueryable();
        if (status.HasValue) query = query.Where(o => o.Status == status.Value);

        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new AdminOrderListItemDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                CustomerName = ((o.User.FirstName ?? "") + " " + (o.User.LastName ?? "")).Trim() != ""
                    ? ((o.User.FirstName ?? "") + " " + (o.User.LastName ?? "")).Trim()
                    : o.ShippingFullName,
                CustomerPhone = o.User.PhoneNumber,
                ItemCount = o.Items.Sum(i => i.Quantity),
                TotalAmount = o.TotalAmount,
                Status = o.Status,
                CreatedAt = o.CreatedAt
            })
            .ToListAsync();

        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminOrderDetailDto>> GetById(int id)
    {
        var order = await _db.Orders
            .Include(o => o.User)
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Include(o => o.Payments)
            .Include(o => o.DiscountCode)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order is null) return NotFound();

        var lastPayment = order.Payments.OrderByDescending(p => p.Id).FirstOrDefault();
        var customerFullName = $"{order.User.FirstName} {order.User.LastName}".Trim();

        return Ok(new AdminOrderDetailDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CreatedAt = order.CreatedAt,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            PaymentStatus = lastPayment?.Status,
            DiscountCode = order.DiscountCode?.Code,

            CustomerName = string.IsNullOrWhiteSpace(customerFullName) ? order.ShippingFullName : customerFullName,
            CustomerPhone = order.User.PhoneNumber,
            ShippingFullName = order.ShippingFullName,
            ShippingPhone = order.ShippingPhone,
            ShippingCity = order.ShippingCity,
            ShippingFullAddress = order.ShippingFullAddress,
            ShippingPostalCode = order.ShippingPostalCode,

            SubTotal = order.SubTotal,
            ShippingCost = order.ShippingCost,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,

            Items = order.Items.Select(i => new AdminOrderItemDto
            {
                ProductName = i.ProductNameSnapshot,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                TotalPrice = i.TotalPrice
            }).ToList(),

            StatusHistory = order.StatusHistory
                .OrderBy(h => h.CreatedAt)
                .Select(h => new AdminOrderStatusHistoryDto { Status = h.Status, Note = h.Note, CreatedAt = h.CreatedAt })
                .ToList()
        });
    }

    // مربوط به دکمه‌های «علامت‌گذاری در حال ارسال / تحویل شد» تو جزئیات سفارش پنل مدیریت.
    // با موفقیت‌آمیز بودن تغییر، یک پیامک اطلاع‌رسانی واقعی (از طریق خط خدماتی اشتراکی ملی‌پیامک،
    // بر اساس الگوی از پیش تایید‌شده Shipped/Delivered) به مشتری ارسال می‌شه.
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequestDto request)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return NotFound();

        string note, bodyId;

        if (request.Status == OrderStatus.Shipped && order.Status == OrderStatus.Processing)
        {
            note = "سفارش توسط مدیر به وضعیت «در حال ارسال» تغییر یافت";
            bodyId = _bodyIds.Shipped;
        }
        else if (request.Status == OrderStatus.Delivered && order.Status == OrderStatus.Shipped)
        {
            note = "سفارش توسط مدیر به وضعیت «تحویل شده» تغییر یافت";
            bodyId = _bodyIds.Delivered;
        }
        else
        {
            return BadRequest(new { message = "این تغییر وضعیت مجاز نیست" });
        }

        order.Status = request.Status;
        order.StatusHistory.Add(new OrderStatusHistory { Status = request.Status, Note = note });
        await _db.SaveChangesAsync();

        // ارسال پیامک صرفا اطلاع‌رسانیه؛ اگه سرویس پیامک قطع باشه نباید جلوی ثبت تغییر وضعیت (که کار اصلیه) رو بگیره
        var smsSent = true;
        string? smsError = null;
        try
        {
            await _smsService.SendOrderStatusAsync(order.ShippingPhone, bodyId, order.OrderNumber);
        }
        catch (Exception ex)
        {
            smsSent = false;
            smsError = ex.Message;
        }

        return Ok(new { message = "وضعیت سفارش بروزرسانی شد", smsSent, smsError });
    }
}
