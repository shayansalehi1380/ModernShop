using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    // مربوط به shop.html: فیلتر دسته/برند/قیمت/امتیاز/موجودی + مرتب‌سازی + صفحه‌بندی ۵۰ تایی
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<ProductListItemDto>>> GetProducts([FromQuery] ProductFilterRequestDto filter)
    {
        var query = _db.Products.Where(p => p.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
            query = query.Where(p => p.Name.Contains(filter.Search));

        if (filter.CategoryIds is { Count: > 0 })
            query = query.Where(p => filter.CategoryIds.Contains(p.CategoryId));

        if (filter.BrandIds is { Count: > 0 })
            query = query.Where(p => p.BrandId != null && filter.BrandIds.Contains(p.BrandId.Value));

        if (filter.MinPrice.HasValue)
            query = query.Where(p => (p.DiscountPrice ?? p.Price) >= filter.MinPrice);

        if (filter.MaxPrice.HasValue)
            query = query.Where(p => (p.DiscountPrice ?? p.Price) <= filter.MaxPrice);

        if (filter.InStockOnly)
            query = query.Where(p => p.StockQuantity > 0);

        if (filter.MinRating.HasValue)
            query = query.Where(p => p.Reviews.Any() && p.Reviews.Average(r => r.Rating) >= filter.MinRating);

        query = filter.SortBy switch
        {
            "cheap" => query.OrderBy(p => p.DiscountPrice ?? p.Price),
            "expensive" => query.OrderByDescending(p => p.DiscountPrice ?? p.Price),
            "bestselling" => query.OrderByDescending(p => p.Reviews.Count),
            "rating" => query.OrderByDescending(p => p.Reviews.Any() ? p.Reviews.Average(r => r.Rating) : 0),
            _ => query.OrderByDescending(p => p.CreatedAt) // newest
        };

        var totalCount = await query.CountAsync();

        var page = Math.Max(filter.Page, 1);
        var pageSize = filter.PageSize <= 0 ? 50 : filter.PageSize; // مطابق منطق فروشگاه: حداکثر ۵۰ در هر صفحه

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                CategoryName = p.Category.Name,
                MainImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                                ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() ?? "",
                Price = p.Price,
                DiscountPrice = p.DiscountPrice,
                AverageRating = p.Reviews.Any() ? Math.Round(p.Reviews.Average(r => r.Rating), 1) : 0,
                ReviewCount = p.Reviews.Count,
                InStock = p.StockQuantity > 0,
                IsVariable = p.Variants.Any(),
                Badge = p.DiscountPrice != null ? "پیشنهاد ویژه"
                        : p.CreatedAt >= DateTime.UtcNow.AddDays(-14) ? "جدید"
                        : null
            })
            .ToListAsync();

        return Ok(new PagedResultDto<ProductListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    // مربوط به جستجوی زنده تو هدر (پیشنهاد لحظه‌ای همراه با عکس، حداکثر ۶ مورد)
    [HttpGet("search-suggestions")]
    public async Task<ActionResult<List<ProductSearchSuggestionDto>>> GetSearchSuggestions([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return Ok(new List<ProductSearchSuggestionDto>());

        var results = await _db.Products
            .Where(p => p.IsActive && p.Name.Contains(q))
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .Select(p => new ProductSearchSuggestionDto
            {
                Name = p.Name,
                Slug = p.Slug,
                ImageUrl = p.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                           ?? p.Images.Select(i => i.ImageUrl).FirstOrDefault() ?? "",
                Price = p.Price,
                DiscountPrice = p.DiscountPrice
            })
            .ToListAsync();

        return Ok(results);
    }

    // مربوط به product.html: گالری، رنگ، مشخصات فنی، نظرات
    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductDetailDto>> GetBySlug(string slug)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Variants)
            .Include(p => p.Specifications)
            .Include(p => p.Reviews.Where(r => r.IsApproved)).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product is null) return NotFound();

        var dto = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Slug = product.Slug,
            Sku = product.Sku,
            Description = product.Description,
            CategoryName = product.Category.Name,
            BrandName = product.Brand?.Name,
            Price = product.Price,
            DiscountPrice = product.DiscountPrice,
            StockQuantity = product.StockQuantity,
            AverageRating = product.Reviews.Any() ? Math.Round(product.Reviews.Average(r => r.Rating), 1) : 0,
            ReviewCount = product.Reviews.Count,
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new ProductImageDto { ImageUrl = i.ImageUrl, IsMain = i.IsMain })
                .ToList(),
            Variants = product.Variants
                .Select(v => new ProductVariantDto { Id = v.Id, Color = v.Color, Size = v.Size, StockQuantity = v.StockQuantity, PriceAdjustment = v.PriceAdjustment, IncludedInDiscount = v.IncludedInDiscount })
                .ToList(),
            Specifications = product.Specifications
                .OrderBy(s => s.DisplayOrder)
                .Select(s => new ProductSpecificationDto { Key = s.Key, Value = s.Value })
                .ToList(),
            Reviews = product.Reviews
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new ProductReviewDto
                {
                    Id = r.Id,
                    UserFullName = $"{r.User.FirstName} {r.User.LastName}".Trim(),
                    Rating = r.Rating,
                    Comment = r.Comment,
                    AdminReply = r.AdminReply,
                    CreatedAt = r.CreatedAt
                })
                .ToList()
        };

        return Ok(dto);
    }

    // مربوط به تب «نظرات» تو product.html
    [HttpPost("reviews")]
    [Authorize]
    public async Task<IActionResult> AddReview([FromBody] CreateReviewRequestDto request, [FromServices] ICurrentUserService currentUser)
    {
        if (request.Rating is < 1 or > 5)
            return BadRequest(new { message = "امتیاز باید بین ۱ تا ۵ باشد" });

        var productExists = await _db.Products.AnyAsync(p => p.Id == request.ProductId);
        if (!productExists) return NotFound(new { message = "محصول یافت نشد" });

        _db.ProductReviews.Add(new ProductReview
        {
            ProductId = request.ProductId,
            UserId = currentUser.UserId!.Value,
            Rating = request.Rating,
            Comment = request.Comment,
            IsApproved = false // بعد از تایید ادمین نمایش داده می‌شه
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "دیدگاه شما ثبت شد و پس از تایید نمایش داده می‌شود" });
    }
}
