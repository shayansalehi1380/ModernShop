using ModernShop.Api.Services;
using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «محصولات» تو پنل مدیریت (admin.html)
[ApiController]
[Route("api/admin/products")]
[Authorize(Policy = "AdminOnly")]
public class AdminProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminProductsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminProductListItemDto>>> GetAll()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                CategoryName = p.Category.Name,
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                StockQuantity = p.StockQuantity,
                IsActive = p.IsActive,
                IsVariable = p.Variants.Any()
            })
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminProductDetailDto>> GetById(int id)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        return Ok(MapToDetailDto(product));
    }

    [HttpPost]
    public async Task<ActionResult<AdminProductDetailDto>> Create([FromBody] SaveProductRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "نام محصول الزامی است" });

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) return BadRequest(new { message = "دسته‌بندی معتبر نیست" });

        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = await ResolveSlugAsync(request.Slug, request.Name, null),
            Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            Price = request.Price,
            DiscountPrice = request.DiscountPrice,
            IsActive = request.IsActive
        };

        ApplyVariantsSpecsAndStock(product, request);
        ApplyImage(product, request.ImageUrl);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return await GetById(product.Id);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminProductDetailDto>> Update(int id, [FromBody] SaveProductRequestDto request)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Specifications)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "نام محصول الزامی است" });

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId);
        if (!categoryExists) return BadRequest(new { message = "دسته‌بندی معتبر نیست" });

        product.Name = request.Name.Trim();
        product.Slug = await ResolveSlugAsync(request.Slug, request.Name, product.Id);
        product.Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        product.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.Price = request.Price;
        product.DiscountPrice = request.DiscountPrice;
        product.IsActive = request.IsActive;

        product.Variants.Clear();
        product.Specifications.Clear();
        ApplyVariantsSpecsAndStock(product, request);

        product.Images.Clear();
        ApplyImage(product, request.ImageUrl);

        await _db.SaveChangesAsync();

        return await GetById(product.Id);
    }

    // «حذف» عمداً soft-delete هست (IsActive=false)، نه DELETE واقعی از دیتابیس؛ چون سفارش‌های قبلی،
    // نظرات و علاقه‌مندی‌ها با DeleteBehavior.Restrict به محصول وصل‌ان (طبق قرارداد AppDbContext:
    // «به‌جای حذف واقعی از IsActive برای غیرفعال‌کردن استفاده کن») و حذف واقعی روی محصولی که قبلا
    // سفارش داده شده، خطای FK می‌ده. غیرفعال کردن دقیقاً همون اثر بیرونی (ناپدید شدن از فروشگاه) رو داره.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static AdminProductDetailDto MapToDetailDto(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Slug = product.Slug,
        Sku = product.Sku,
        Description = product.Description,
        CategoryId = product.CategoryId,
        BrandId = product.BrandId,
        Price = product.Price,
        DiscountPrice = product.DiscountPrice,
        StockQuantity = product.StockQuantity,
        IsActive = product.IsActive,
        ImageUrl = product.Images.FirstOrDefault(i => i.IsMain)?.ImageUrl
                   ?? product.Images.FirstOrDefault()?.ImageUrl,
        Specifications = product.Specifications
            .OrderBy(s => s.DisplayOrder)
            .Select(s => new AdminProductSpecDto { Key = s.Key, Value = s.Value })
            .ToList(),
        Variants = product.Variants
            .Select(v => new AdminProductVariantDto
            {
                Color = v.Color,
                Size = v.Size,
                StockQuantity = v.StockQuantity,
                PriceAdjustment = v.PriceAdjustment
            })
            .ToList()
    };

    private static void ApplyVariantsSpecsAndStock(Product product, SaveProductRequestDto request)
    {
        var variants = (request.Variants ?? new List<AdminProductVariantDto>())
            .Where(v => !string.IsNullOrWhiteSpace(v.Color) || !string.IsNullOrWhiteSpace(v.Size))
            .Select(v => new ProductVariant
            {
                Color = string.IsNullOrWhiteSpace(v.Color) ? null : v.Color.Trim(),
                Size = string.IsNullOrWhiteSpace(v.Size) ? "-" : v.Size.Trim(),
                StockQuantity = v.StockQuantity,
                PriceAdjustment = v.PriceAdjustment
            })
            .ToList();

        foreach (var variant in variants) product.Variants.Add(variant);

        var specs = (request.Specifications ?? new List<AdminProductSpecDto>())
            .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !string.IsNullOrWhiteSpace(s.Value))
            .Select((s, index) => new ProductSpecification { Key = s.Key.Trim(), Value = s.Value.Trim(), DisplayOrder = index })
            .ToList();

        foreach (var spec in specs) product.Specifications.Add(spec);

        // اگه محصول تنوع (رنگ/سایز) داره، موجودی کل رو خودمون از مجموع موجودی تنوع‌ها حساب می‌کنیم
        // تا هیچ‌وقت با موجودی تک‌تک تنوع‌ها ناهماهنگ نشه؛ فقط برای محصول ساده مقدار ورودی فرم رو می‌ذاریم.
        product.StockQuantity = variants.Count > 0 ? variants.Sum(v => v.StockQuantity) : request.StockQuantity;
    }

    private static void ApplyImage(Product product, string? imageUrl)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            product.Images.Add(new ProductImage { ImageUrl = imageUrl.Trim(), IsMain = true, DisplayOrder = 0 });
        }
    }

    private async Task<string> ResolveSlugAsync(string? requestedSlug, string name, int? excludingProductId)
    {
        var baseSlug = SlugHelper.Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? name : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "product";

        var slug = baseSlug;
        var suffix = 2;
        // excludingProductId ?? 0 یعنی «هیچ محصولی رو مستثنی نکن» چون Id واقعی هیچ‌وقت صفر نیست
        while (await _db.Products.AnyAsync(p => p.Slug == slug && p.Id != (excludingProductId ?? 0)))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }
}
