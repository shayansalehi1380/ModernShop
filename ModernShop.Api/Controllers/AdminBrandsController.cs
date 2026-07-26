using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «برندها» تو پنل مدیریت (admin.html)
[ApiController]
[Route("api/admin/brands")]
[Authorize(Policy = "AdminOnly")]
public class AdminBrandsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminBrandsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminBrandDto>>> GetAll()
    {
        var brands = await _db.Brands.AsNoTracking()
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();

        return Ok(brands.Select(MapToDto));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminBrandDto>> GetById(int id)
    {
        var brand = await _db.Brands.FindAsync(id);
        if (brand is null) return NotFound();

        return Ok(MapToDto(brand));
    }

    [HttpPost]
    public async Task<ActionResult<AdminBrandDto>> Create([FromBody] SaveBrandRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "نام برند الزامی است" });

        var brand = new Brand
        {
            Name = request.Name.Trim(),
            LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim(),
            IsFeatured = request.IsFeatured,
            DisplayOrder = request.DisplayOrder
        };

        _db.Brands.Add(brand);
        await _db.SaveChangesAsync();

        return Ok(MapToDto(brand));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminBrandDto>> Update(int id, [FromBody] SaveBrandRequestDto request)
    {
        var brand = await _db.Brands.FindAsync(id);
        if (brand is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "نام برند الزامی است" });

        brand.Name = request.Name.Trim();
        brand.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        brand.IsFeatured = request.IsFeatured;
        brand.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync();

        return Ok(MapToDto(brand));
    }

    // برخلاف بنر، برند از طریق Products.BrandId (با DeleteBehavior.Restrict) به محصولات وصله؛
    // حذف واقعیِ برندی که هنوز محصولی بهش وصله با خطای FK شکست می‌خوره، پس قبلش چک می‌کنیم
    // و یه پیام قابل‌فهم برمی‌گردونیم به‌جای خطای ۵۰۰ خام.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var brand = await _db.Brands.FindAsync(id);
        if (brand is null) return NotFound();

        var inUse = await _db.Products.AnyAsync(p => p.BrandId == id);
        if (inUse)
            return BadRequest(new { message = "این برند به یک یا چند محصول متصل است؛ ابتدا برند آن محصولات را تغییر دهید یا خالی کنید." });

        _db.Brands.Remove(brand);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static AdminBrandDto MapToDto(Brand b) => new()
    {
        Id = b.Id,
        Name = b.Name,
        LogoUrl = b.LogoUrl,
        IsFeatured = b.IsFeatured,
        DisplayOrder = b.DisplayOrder
    };
}
