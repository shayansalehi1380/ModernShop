using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «تنظیمات» تو پنل مدیریت - فعلا فقط هزینه ارسال پیش‌پرداخت
[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = "AdminOnly")]
public class AdminSettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public AdminSettingsController(AppDbContext db) => _db = db;

    [HttpGet("shipping-cost")]
    public async Task<ActionResult<ShippingCostSettingDto>> GetShippingCost()
    {
        var setting = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "PrepaidShippingCost");

        var cost = setting is not null && decimal.TryParse(setting.Value, out var parsed) ? parsed : 95000;

        return Ok(new ShippingCostSettingDto { PrepaidShippingCost = cost });
    }

    [HttpPut("shipping-cost")]
    public async Task<ActionResult<ShippingCostSettingDto>> UpdateShippingCost([FromBody] UpdateShippingCostRequestDto request)
    {
        if (request.PrepaidShippingCost < 0)
            return BadRequest(new { message = "هزینه ارسال نمی‌تواند منفی باشد" });

        var setting = await _db.AppSettings.FirstOrDefaultAsync(s => s.Key == "PrepaidShippingCost");
        if (setting is null)
        {
            setting = new AppSetting { Key = "PrepaidShippingCost", Description = "هزینه ارسال پیش‌پرداخت (تومان) که تو چک‌اوت وقتی مشتری «ارسال با پست پیشتاز» رو انتخاب می‌کنه ازش گرفته می‌شه" };
            _db.AppSettings.Add(setting);
        }
        setting.Value = request.PrepaidShippingCost.ToString();

        await _db.SaveChangesAsync();

        return Ok(new ShippingCostSettingDto { PrepaidShippingCost = request.PrepaidShippingCost });
    }
}
