using ModernShop.Core.DTOs;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

// مربوط به page.html: نمایش عمومی صفحات ثابت فوتر (داستان ما، تماس با ما، سوالات متداول، شرایط بازگشت کالا)
[ApiController]
[Route("api/static-pages")]
public class StaticPagesController : ControllerBase
{
    private readonly AppDbContext _db;

    public StaticPagesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<StaticPageDto>> GetBySlug(string slug)
    {
        var page = await _db.StaticPages.FirstOrDefaultAsync(p => p.Slug == slug);
        if (page is null) return NotFound();

        return Ok(new StaticPageDto { Slug = page.Slug, Title = page.Title, Content = page.Content });
    }
}
