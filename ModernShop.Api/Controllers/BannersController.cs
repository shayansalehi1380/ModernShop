using ModernShop.Core.Enums;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

// مربوط به اسلایدر بالای صفحه اصلی (Type=Hero) و دو بنر ثابت تبلیغاتی زیر نوار برندها (Type=Fixed)
[ApiController]
[Route("api/banners")]
public class BannersController : ControllerBase
{
    private readonly AppDbContext _db;
    public BannersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetActive() => Ok(await GetActiveByTypeAsync(BannerType.Hero));

    [HttpGet("fixed")]
    public async Task<IActionResult> GetFixed() => Ok(await GetActiveByTypeAsync(BannerType.Fixed));

    private async Task<object> GetActiveByTypeAsync(BannerType type)
    {
        var now = DateTime.UtcNow;

        return await _db.Banners.AsNoTracking()
            .Where(b => b.Type == type)
            .Where(b => b.IsActive)
            .Where(b => (b.StartsAt == null || b.StartsAt <= now) && (b.EndsAt == null || b.EndsAt >= now))
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new { b.Id, b.ImageUrl, b.MobileImageUrl, b.Title, b.LinkUrl })
            .ToListAsync();
    }
}
