using ModernShop.Core.DTOs;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

// مربوط به بخش «صفحات ثابت» تو پنل مدیریت (admin.html)
// این صفحات (داستان ما/تماس با ما/سوالات متداول/شرایط بازگشت کالا) از پیش با اسلاگ ثابت وجود دارن؛
// از پنل فقط عنوان و متنشون قابل ویرایشه، نه افزودن/حذف صفحه.
[ApiController]
[Route("api/admin/static-pages")]
[Authorize(Policy = "AdminOnly")]
public class AdminStaticPagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminStaticPagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminStaticPageListItemDto>>> GetAll()
    {
        var pages = await _db.StaticPages
            .OrderBy(p => p.Id)
            .Select(p => new AdminStaticPageListItemDto { Id = p.Id, Slug = p.Slug, Title = p.Title, UpdatedAt = p.UpdatedAt })
            .ToListAsync();

        return Ok(pages);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<StaticPageDto>> GetBySlug(string slug)
    {
        var page = await _db.StaticPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page is null) return NotFound();

        return Ok(new StaticPageDto { Slug = page.Slug, Title = page.Title, Content = page.Content });
    }

    [HttpPut("{slug}")]
    public async Task<ActionResult<StaticPageDto>> Update(string slug, [FromBody] UpdateStaticPageRequestDto request)
    {
        var page = await _db.StaticPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "عنوان صفحه الزامی است" });

        page.Title = request.Title.Trim();
        page.Content = (request.Content ?? "").Trim();
        page.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new StaticPageDto { Slug = page.Slug, Title = page.Title, Content = page.Content });
    }
}
