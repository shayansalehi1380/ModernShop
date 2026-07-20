using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

// مربوط به فیلتر دسته‌بندی تو shop.html و ویترین دسته‌بندی صفحه اصلی
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _db.Categories
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Slug,
                c.ImageUrl,
                c.ParentCategoryId,
                ProductCount = c.Products.Count(p => p.IsActive)
            })
            .ToListAsync();

        return Ok(categories);
    }
}

// مربوط به فیلتر برند تو shop.html و نوار برندهای صفحه اصلی
[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly AppDbContext _db;
    public BrandsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool featuredOnly = false)
    {
        var query = _db.Brands.AsQueryable();
        if (featuredOnly) query = query.Where(b => b.IsFeatured);

        var brands = await query
            .OrderBy(b => b.DisplayOrder)
            .Select(b => new { b.Id, b.Name, b.LogoUrl })
            .ToListAsync();

        return Ok(brands);
    }
}
