using System.Text.Json;
using System.Text.RegularExpressions;
using ModernShop.Api.Services;
using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ModernShop.Api.Controllers;

// اتصال فروشگاه به ترب (torob.com) طبق مستندات TorobAPI نسخه ۳. این اندپوینت با هیچ‌کدوم
// از سیستم‌های احراز هویت دیگه‌ی سایت (JWT مشتری/ادمین) کار نمی‌کنه - فقط با توکن مخصوص
// ترب (هدر X-Torob-Token، امضاشده با EdDSA) که تو TorobTokenValidator چک می‌شه.
[ApiController]
[Route("torob_api/v3/products")]
public class TorobController : ControllerBase
{
    private const int PageSize = 100;

    private readonly AppDbContext _db;
    private readonly TorobTokenValidator _tokenValidator;
    private readonly TorobSettings _settings;

    public TorobController(AppDbContext db, TorobTokenValidator tokenValidator, IOptions<TorobSettings> settings)
    {
        _db = db;
        _tokenValidator = tokenValidator;
        _settings = settings.Value;
    }

    // بدنه‌ی درخواست رو خودمون دستی می‌خونیم و parse می‌کنیم (نه با [FromBody] معمولی)، تا حتی
    // وقتی JSON بدنه خراب/خالیه، خطا دقیقاً با همون فرمتی که مستندات ترب می‌خواد
    // ({"error": "..."}) برگرده، نه فرمت پیش‌فرض ProblemDetails خود ASP.NET Core
    [HttpPost]
    public async Task<IActionResult> GetProducts([FromHeader(Name = "X-Torob-Token")] string? torobToken)
    {
        if (!_tokenValidator.Validate(torobToken, out var tokenError))
            return Unauthorized(new TorobErrorDto { Error = tokenError });

        TorobRequestDto? request;
        try
        {
            using var reader = new StreamReader(Request.Body);
            var bodyText = await reader.ReadToEndAsync();
            request = string.IsNullOrWhiteSpace(bodyText)
                ? null
                : JsonSerializer.Deserialize<TorobRequestDto>(bodyText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException)
        {
            return BadRequest(new TorobErrorDto { Error = "request body is not valid JSON" });
        }

        if (request is null)
            return BadRequest(new TorobErrorDto { Error = "request body is empty or invalid" });

        var hasUrls = request.PageUrls is { Count: > 0 };
        var hasUniques = request.PageUniques is { Count: > 0 };
        var hasPagination = request.Page.HasValue && !string.IsNullOrWhiteSpace(request.Sort);

        // دقیقاً باید یکی از سه حالت مشخص باشه؛ طبق تاکید مستندات ترب هیچ مقدار پیش‌فرضی
        // برای هیچ پارامتری در نظر گرفته نمی‌شه
        var modesProvided = (hasUrls ? 1 : 0) + (hasUniques ? 1 : 0) + (hasPagination ? 1 : 0);
        if (modesProvided != 1)
            return BadRequest(new TorobErrorDto { Error = "exactly one of page_urls, page_uniques, or (page and sort) must be provided" });

        if (hasPagination)
        {
            if (request.Page!.Value < 1)
                return BadRequest(new TorobErrorDto { Error = "page must start from 1" });
            if (request.Sort != "date_added_desc" && request.Sort != "date_updated_desc")
                return BadRequest(new TorobErrorDto { Error = "sort must be either date_added_desc or date_updated_desc" });
        }

        var allEntries = await BuildAllEntriesAsync();

        if (hasUrls)
        {
            var slugs = request.PageUrls!
                .Select(ExtractSlugFromUrl)
                .Where(s => s is not null)
                .Select(s => s!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matched = allEntries.Where(e => slugs.Contains(e.Slug)).Select(e => e.Dto).ToList();
            return Ok(new TorobResultDto { CurrentPage = 1, Total = matched.Count, MaxPages = 1, Products = matched });
        }

        if (hasUniques)
        {
            var uniques = request.PageUniques!.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var matched = allEntries.Where(e => uniques.Contains(e.Dto.PageUnique)).Select(e => e.Dto).ToList();
            return Ok(new TorobResultDto { CurrentPage = 1, Total = matched.Count, MaxPages = 1, Products = matched });
        }

        // حالت صفحه‌بندی کامل کاتالوگ - محصولات متغیر از قبل به‌صورت جداگانه (هر تنوع = یک
        // ورودی) تو allEntries باز شدن، پس صفحه‌بندی رو باید رو همین لیست بازشده انجام بدیم،
        // نه رو تعداد ردیف‌های واقعی جدول Products
        var ordered = allEntries.OrderByDescending(e => e.SortKey).ToList();
        var total = ordered.Count;
        var maxPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)PageSize);
        var pageItems = ordered.Skip((request.Page!.Value - 1) * PageSize).Take(PageSize).Select(e => e.Dto).ToList();

        return Ok(new TorobResultDto
        {
            CurrentPage = request.Page.Value,
            Total = total,
            MaxPages = maxPages,
            Products = pageItems
        });
    }

    private record TorobEntry(string Slug, DateTime SortKey, TorobProductDto Dto);

    private async Task<List<TorobEntry>> BuildAllEntriesAsync()
    {
        // فقط محصولات فعال (IsActive) - محصولات غیرفعال/حذف‌شده نباید اصلاً تو خروجی ترب باشن
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Specifications)
            .Include(p => p.Variants)
            .ToListAsync();

        var entries = new List<TorobEntry>();
        var baseUrl = _settings.SiteBaseUrl.TrimEnd('/');

        foreach (var product in products)
        {
            var imageLinks = product.Images
                .OrderByDescending(i => i.IsMain)
                .ThenBy(i => i.DisplayOrder)
                .Select(i => AbsoluteUrl(baseUrl, i.ImageUrl))
                .ToList();

            var pageUrl = $"{baseUrl}/product?slug={Uri.EscapeDataString(product.Slug)}";
            var categoryName = product.Category?.Name;
            var shortDesc = TruncateText(StripHtml(product.Description), 500);
            var baseSpec = product.Specifications.ToDictionary(s => s.Key, s => s.Value);
            var dateAdded = ToIso8601Utc(product.CreatedAt);

            if (product.Variants.Count == 0)
            {
                var (price, oldPrice) = ComputePrices(product, null);
                entries.Add(new TorobEntry(product.Slug, product.CreatedAt, new TorobProductDto
                {
                    PageUnique = product.Id.ToString(),
                    PageUrl = pageUrl,
                    ProductGroupId = null,
                    Title = product.Name,
                    CurrentPrice = price,
                    OldPrice = oldPrice,
                    Availability = product.StockQuantity > 0,
                    CategoryName = categoryName,
                    ImageLinks = imageLinks,
                    ShortDesc = shortDesc,
                    Spec = new Dictionary<string, string>(baseSpec),
                    Guarantee = product.Guarantee,
                    DateAdded = dateAdded,
                    DateUpdated = dateAdded
                }));
                continue;
            }

            // محصول متغیر (رنگ/سایز): هر تنوع یک ورودی جدا با page_unique خودش می‌شه، ولی
            // همه‌شون product_group_id مشترک (شناسه‌ی محصول) دارن تا ترب گروه‌بندی‌شون کنه.
            // چون تو دیتابیس این سایت برای هر تنوع عکس جدا ذخیره نمی‌شه، همه از عکس‌های خود
            // محصول استفاده می‌کنن.
            foreach (var variant in product.Variants)
            {
                var (price, oldPrice) = ComputePrices(product, variant);
                var variantSpec = new Dictionary<string, string>(baseSpec);
                if (!string.IsNullOrWhiteSpace(variant.Color)) variantSpec["رنگ"] = variant.Color;
                if (!string.IsNullOrWhiteSpace(variant.Size) && variant.Size != "-") variantSpec["سایز"] = variant.Size;

                var variantLabel = string.Join("، ", new[] { variant.Color, variant.Size == "-" ? null : variant.Size }
                    .Where(s => !string.IsNullOrWhiteSpace(s)));
                var title = string.IsNullOrWhiteSpace(variantLabel) ? product.Name : $"{product.Name} ({variantLabel})";

                entries.Add(new TorobEntry(product.Slug, product.CreatedAt, new TorobProductDto
                {
                    PageUnique = $"{product.Id}_{variant.Id}",
                    PageUrl = pageUrl,
                    ProductGroupId = product.Id.ToString(),
                    Title = title,
                    CurrentPrice = price,
                    OldPrice = oldPrice,
                    Availability = variant.StockQuantity > 0,
                    CategoryName = categoryName,
                    ImageLinks = imageLinks,
                    ShortDesc = shortDesc,
                    Spec = variantSpec,
                    Guarantee = product.Guarantee,
                    DateAdded = dateAdded,
                    DateUpdated = dateAdded
                }));
            }
        }

        return entries;
    }

    // همون فرمول قیمت نهایی که تو product.html و پنل مدیریت استفاده می‌شه: اگه محصول تخفیف
    // داشته باشه و تنوع (در صورت وجود) از اون تخفیف مستثنی نشده باشه، قیمت پایه تخفیف‌خورده
    // به‌علاوه‌ی اختلاف قیمت تنوع؛ وگرنه قیمت اصلی به‌علاوه‌ی اختلاف قیمت تنوع
    private static (long price, long? oldPrice) ComputePrices(Product product, ProductVariant? variant)
    {
        var adjustment = variant?.PriceAdjustment ?? 0;
        var includedInDiscount = variant?.IncludedInDiscount ?? true;
        var applyDiscount = product.DiscountPrice.HasValue && includedInDiscount;
        var basePrice = applyDiscount ? product.DiscountPrice!.Value : product.Price;
        var finalPrice = (long)Math.Round(basePrice + adjustment);
        long? oldPrice = applyDiscount ? (long)Math.Round(product.Price + adjustment) : null;
        return (finalPrice, oldPrice);
    }

    private static string AbsoluteUrl(string baseUrl, string relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute)) return relativeOrAbsolute;
        if (relativeOrAbsolute.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            relativeOrAbsolute.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return relativeOrAbsolute;
        return relativeOrAbsolute.StartsWith('/') ? $"{baseUrl}{relativeOrAbsolute}" : $"{baseUrl}/{relativeOrAbsolute}";
    }

    private static string? StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;
        var text = Regex.Replace(html, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text);
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static string? TruncateText(string? text, int maxLen)
    {
        if (string.IsNullOrEmpty(text)) return text;
        return text.Length <= maxLen ? text : text[..maxLen];
    }

    private static string ToIso8601Utc(DateTime utcDateTime)
    {
        var dt = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Unspecified);
        return new DateTimeOffset(dt, TimeSpan.Zero).ToString("yyyy-MM-ddTHH:mm:sszzz");
    }

    private static string? ExtractSlugFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url, UriKind.Absolute);
            var query = QueryHelpers.ParseQuery(uri.Query);
            return query.TryGetValue("slug", out var values) ? values.ToString() : null;
        }
        catch
        {
            return null;
        }
    }
}
