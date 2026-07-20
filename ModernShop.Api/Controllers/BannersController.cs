using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

// مربوط به اسلایدر بالای صفحه اصلی
[ApiController]
[Route("api/banners")]
public class BannersController : ControllerBase
{
    private readonly AppDbContext _db;
    public BannersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetActive()
    {
        var now = DateTime.UtcNow;

        var banners = await _db.Banners
            .Where(b => b.IsActive)
            .Where(b => (b.StartsAt == null || b.StartsAt <= now) && (b.EndsAt == null || b.EndsAt >= now))
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new { b.Id, b.ImageUrl, b.Title, b.LinkUrl })
            .ToListAsync();

        return Ok(banners);
    }
}
