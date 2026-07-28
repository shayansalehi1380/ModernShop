using ModernShop.Core.DTOs;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به مدیریت عکس دسته‌بندی‌ها تو پنل مدیریت (همون عکس‌هایی که تو شیت «دسته‌بندی‌ها»ی
// منوی پایین موبایل نشون داده می‌شن). خود دسته‌بندی‌ها (اسم/زیردسته/ترتیب) از جای دیگه‌ای
// (سید دیتابیس) میان و این‌جا فقط عکسشون قابل تغییره. آپلود فایل از همون endpoint مشترک
// آپلود عکس محصول استفاده می‌کنه (api/admin/products/upload-image) - دقیقاً همون الگویی
// که لوگوی برندها هم ازش استفاده می‌کنن.
[ApiController]
[Route("api/admin/categories")]
[Authorize(Policy = "AdminOnly")]
public class AdminCategoriesController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminCategoriesController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminCategoryDto>>> GetAll()
    {
        var categories = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new AdminCategoryDto { Id = c.Id, Name = c.Name, ImageUrl = c.ImageUrl })
            .ToListAsync();

        return Ok(categories);
    }

    [HttpPut("{id}/image")]
    public async Task<IActionResult> UpdateImage(int id, [FromBody] UpdateCategoryImageRequestDto request)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return NotFound();

        category.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        await _db.SaveChangesAsync();

        return Ok(new AdminCategoryDto { Id = category.Id, Name = category.Name, ImageUrl = category.ImageUrl });
    }
}
