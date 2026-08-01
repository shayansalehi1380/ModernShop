using ModernShop.Core.DTOs;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// تنظیمات عمومی سایت که تو صفحات مشتری (چک‌اوت) لازمه، بدون نیاز به لاگین ادمین
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;
    public SettingsController(AppDbContext db) => _db = db;

    [HttpGet("shipping-cost")]
    public async Task<ActionResult<ShippingCostSettingDto>> GetShippingCost()
    {
        var setting = await _db.AppSettings.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == "PrepaidShippingCost");

        var cost = setting is not null && decimal.TryParse(setting.Value, out var parsed) ? parsed : 95000;

        return Ok(new ShippingCostSettingDto { PrepaidShippingCost = cost });
    }
}
