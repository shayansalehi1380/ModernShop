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
        ApplyImages(product, request.ImageUrl, request.GalleryImageUrls);

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

        product.Specifications.Clear();
        foreach (var spec in BuildSpecifications(request))
            product.Specifications.Add(spec);

        await ReconcileVariantsAsync(product, request);

        product.Images.Clear();
        ApplyImages(product, request.ImageUrl, request.GalleryImageUrls);

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

    // برای برگردوندن محصولی که قبلا از پنل «حذف» (غیرفعال) شده
    [HttpPut("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return NotFound();

        product.IsActive = true;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // آپلود فایل عکس از پنل مدیریت (به‌جای وارد کردن لینک عکس)؛ فایل رو داخل wwwroot/uploads/products
    // ذخیره می‌کنه و آدرس نسبی رو برمی‌گردونه تا تو همون فیلد آدرس تصویر استفاده بشه
    [HttpPost("upload-image")]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> UploadImage(IFormFile file, [FromServices] IWebHostEnvironment env)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "فایلی انتخاب نشده است" });

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
            return BadRequest(new { message = "فقط فایل‌های تصویری (jpg, png, webp, gif) مجاز هستند" });

        var uploadsDir = Path.Combine(env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(uploadsDir);

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        return Ok(new { url = $"/uploads/products/{fileName}" });
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
        GalleryImageUrls = product.Images
            .Where(i => !i.IsMain)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.ImageUrl)
            .ToList(),
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
                PriceAdjustment = v.PriceAdjustment,
                IncludedInDiscount = v.IncludedInDiscount
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
                PriceAdjustment = v.PriceAdjustment,
                IncludedInDiscount = v.IncludedInDiscount
            })
            .ToList();

        foreach (var variant in variants) product.Variants.Add(variant);

        foreach (var spec in BuildSpecifications(request)) product.Specifications.Add(spec);

        // اگه محصول تنوع (رنگ/سایز) داره، موجودی کل رو خودمون از مجموع موجودی تنوع‌ها حساب می‌کنیم
        // تا هیچ‌وقت با موجودی تک‌تک تنوع‌ها ناهماهنگ نشه؛ فقط برای محصول ساده مقدار ورودی فرم رو می‌ذاریم.
        product.StockQuantity = variants.Count > 0 ? variants.Sum(v => v.StockQuantity) : request.StockQuantity;
    }

    private static List<ProductSpecification> BuildSpecifications(SaveProductRequestDto request) =>
        (request.Specifications ?? new List<AdminProductSpecDto>())
            .Where(s => !string.IsNullOrWhiteSpace(s.Key) && !string.IsNullOrWhiteSpace(s.Value))
            .Select((s, index) => new ProductSpecification { Key = s.Key.Trim(), Value = s.Value.Trim(), DisplayOrder = index })
            .ToList();

    // موقع ویرایش یک محصول، به‌جای پاک‌کردن کامل تنوع‌های قبلی و ساختنشون از صفر (کاری که این
    // متد قبلا با Clear()+ApplyVariantsSpecsAndStock انجام می‌داد)، تنوع‌هایی که هنوز با همون
    // رنگ/سایز تو درخواست جدید هم هستن رو سرجاشون آپدیت می‌کنیم، نه این‌که حذف و دوباره اضافه
    // بشن. چرا؟ چون اگه یکی از تنوع‌های قدیمی همین الان تو سبد خرید یک مشتری باشه
    // (CartItem.ProductVariantId بهش اشاره می‌کنه)، حذف واقعی اون ردیف از دیتابیس با خطای
    // FK_CartItems_ProductVariants_ProductVariantId شکست می‌خورد و کل ذخیره محصول کرش می‌کرد —
    // حتی برای ویرایش‌های کاملاً بی‌ربط به تنوع‌ها (مثل تغییر توضیحات محصول).
    // فقط تنوع‌هایی که واقعاً از فرم ادمین حذف شدن (دیگه تو درخواست نیستن) حذف می‌شن؛ برای
    // اون‌ها هم اول هر سبد خریدی که بهشون اشاره کرده پاک می‌شه تا خطای FK پیش نیاد.
    private async Task ReconcileVariantsAsync(Product product, SaveProductRequestDto request)
    {
        var incoming = (request.Variants ?? new List<AdminProductVariantDto>())
            .Where(v => !string.IsNullOrWhiteSpace(v.Color) || !string.IsNullOrWhiteSpace(v.Size))
            .ToList();

        var existingByKey = product.Variants
            .GroupBy(v => VariantKey(v.Color, v.Size))
            .ToDictionary(g => g.Key, g => g.First());
        var matchedKeys = new HashSet<string>();

        foreach (var dto in incoming)
        {
            var key = VariantKey(dto.Color, dto.Size);
            matchedKeys.Add(key);

            if (existingByKey.TryGetValue(key, out var existing))
            {
                existing.StockQuantity = dto.StockQuantity;
                existing.PriceAdjustment = dto.PriceAdjustment;
                existing.IncludedInDiscount = dto.IncludedInDiscount;
            }
            else
            {
                product.Variants.Add(new ProductVariant
                {
                    Color = string.IsNullOrWhiteSpace(dto.Color) ? null : dto.Color.Trim(),
                    Size = string.IsNullOrWhiteSpace(dto.Size) ? "-" : dto.Size.Trim(),
                    StockQuantity = dto.StockQuantity,
                    PriceAdjustment = dto.PriceAdjustment,
                    IncludedInDiscount = dto.IncludedInDiscount
                });
            }
        }

        var removedVariants = existingByKey
            .Where(kv => !matchedKeys.Contains(kv.Key))
            .Select(kv => kv.Value)
            .ToList();

        if (removedVariants.Count > 0)
        {
            var removedIds = removedVariants.Select(v => v.Id).ToList();
            var staleCartItems = await _db.CartItems
                .Where(ci => ci.ProductVariantId != null && removedIds.Contains(ci.ProductVariantId.Value))
                .ToListAsync();
            if (staleCartItems.Count > 0) _db.CartItems.RemoveRange(staleCartItems);

            foreach (var variant in removedVariants) product.Variants.Remove(variant);
        }

        product.StockQuantity = product.Variants.Count > 0 ? product.Variants.Sum(v => v.StockQuantity) : request.StockQuantity;
    }

    private static string VariantKey(string? color, string? size) =>
        $"{color?.Trim().ToLowerInvariant()}|{(string.IsNullOrWhiteSpace(size) ? "-" : size.Trim().ToLowerInvariant())}";

    private static void ApplyImages(Product product, string? mainImageUrl, List<string>? galleryImageUrls)
    {
        var order = 0;
        if (!string.IsNullOrWhiteSpace(mainImageUrl))
            product.Images.Add(new ProductImage { ImageUrl = mainImageUrl.Trim(), IsMain = true, DisplayOrder = order++ });

        foreach (var url in (galleryImageUrls ?? new List<string>()).Where(u => !string.IsNullOrWhiteSpace(u)))
            product.Images.Add(new ProductImage { ImageUrl = url.Trim(), IsMain = false, DisplayOrder = order++ });
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
