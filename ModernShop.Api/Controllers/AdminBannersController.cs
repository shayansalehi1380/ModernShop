using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «بنرها» تو پنل مدیریت: هم اسلایدر بالای صفحه اصلی (Type=Hero)
// هم دو بنر ثابت تبلیغاتی زیر نوار برندها (Type=Fixed)
[ApiController]
[Route("api/admin/banners")]
[Authorize(Policy = "AdminOnly")]
public class AdminBannersController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminBannersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminBannerDto>>> GetAll()
    {
        var banners = await _db.Banners.AsNoTracking()
            .OrderBy(b => b.Type).ThenBy(b => b.DisplayOrder)
            .ToListAsync();

        return Ok(banners.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminBannerDto>> GetById(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner is null) return NotFound();

        return Ok(MapToDto(banner));
    }

    [HttpPost]
    public async Task<ActionResult<AdminBannerDto>> Create([FromBody] SaveBannerRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageUrl))
            return BadRequest(new { message = "تصویر بنر الزامی است" });

        var banner = new Banner
        {
            ImageUrl = request.ImageUrl.Trim(),
            MobileImageUrl = string.IsNullOrWhiteSpace(request.MobileImageUrl) ? null : request.MobileImageUrl.Trim(),
            Type = request.Type,
            Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim(),
            LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt
        };

        _db.Banners.Add(banner);
        await _db.SaveChangesAsync();

        return Ok(MapToDto(banner));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminBannerDto>> Update(int id, [FromBody] SaveBannerRequestDto request)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.ImageUrl))
            return BadRequest(new { message = "تصویر بنر الزامی است" });

        banner.ImageUrl = request.ImageUrl.Trim();
        banner.MobileImageUrl = string.IsNullOrWhiteSpace(request.MobileImageUrl) ? null : request.MobileImageUrl.Trim();
        banner.Type = request.Type;
        banner.Title = string.IsNullOrWhiteSpace(request.Title) ? null : request.Title.Trim();
        banner.LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl.Trim();
        banner.DisplayOrder = request.DisplayOrder;
        banner.IsActive = request.IsActive;
        banner.StartsAt = request.StartsAt;
        banner.EndsAt = request.EndsAt;

        await _db.SaveChangesAsync();

        return Ok(MapToDto(banner));
    }

    // بنرها به هیچ جدول دیگه‌ای وصل نیستن، پس حذف واقعی امنه
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner is null) return NotFound();

        _db.Banners.Remove(banner);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static AdminBannerDto MapToDto(Banner b) => new()
    {
        Id = b.Id,
        ImageUrl = b.ImageUrl,
        MobileImageUrl = b.MobileImageUrl,
        Type = b.Type,
        Title = b.Title,
        LinkUrl = b.LinkUrl,
        DisplayOrder = b.DisplayOrder,
        IsActive = b.IsActive,
        StartsAt = b.StartsAt,
        EndsAt = b.EndsAt
    };
}
