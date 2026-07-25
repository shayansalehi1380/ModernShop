using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using ModernShop.Infrastructure.Data;

namespace ModernShop.Api.Services;

/// <summary>
/// ربات‌های هوش مصنوعی (GPTBot، ClaudeBot و ...) و بعضی موتورهای جستجو، برخلاف Google،
/// جاوااسکریپت صفحه رو اجرا نمی‌کنن - فقط همون HTML خامی که سرور می‌فرسته رو می‌خونن. چون
/// product.html و blog-post.html محتواشون رو بعد از لود صفحه با جاوااسکریپت از API می‌گیرن،
/// این ربات‌ها یه اسکلت خالی می‌بینن، نه اسم/قیمت/توضیحات واقعی محصول یا مطلب.
///
/// این سرویس همون فایل HTML استاتیک رو از دیسک می‌خونه ولی قبل از فرستادنش، عنوان صفحه،
/// توضیح متا، تگ‌های Open Graph و داده‌ی ساخت‌یافته‌ی Schema.org (Product / BlogPosting) رو
/// با اطلاعات واقعی از دیتابیس جایگزین می‌کنه - دقیقاً همون‌جاهایی که جاوااسکریپت کلاینت هم
/// (بعد از لود کامل) همین مقادیر رو ست می‌کنه، پس هیچ تناقضی بین نسخه‌ی سرور و کلاینت نیست.
/// ظاهر و رفتار صفحه برای کاربر واقعی هیچ فرقی نمی‌کنه؛ این فقط اون بخشی از HTML رو که قبلاً
/// خالی می‌موند تا جاوااسکریپت پرش کنه، از قبل با محتوای درست پر می‌کنه.
/// </summary>
public class SeoPageRenderer
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public SeoPageRenderer(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<bool> TryRenderProductAsync(HttpContext context, string slug)
    {
        var product = await _db.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Include(p => p.Images)
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsActive);

        if (product is null) return false;

        var template = await ReadTemplateAsync("product.html");
        if (template is null) return false;

        var baseUrl = GetBaseUrl(context);
        var pageUrl = $"{baseUrl}/product?slug={Uri.EscapeDataString(slug)}";
        var description = Truncate(StripHtml(product.Description), 160);
        var images = product.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => AbsoluteUrl(baseUrl, i.ImageUrl))
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .ToList();
        var mainImage = images.FirstOrDefault() ?? "";
        var title = $"دُرین مارکت | {product.Name}";

        var offer = new Dictionary<string, object?>
        {
            ["@type"] = "Offer",
            ["url"] = pageUrl,
            ["priceCurrency"] = "IRR",
            // قیمت‌های سایت به تومانه؛ واحد رسمی ایران تو Schema.org ریاله (۱ تومان = ۱۰ ریال) -
            // این تبدیل فقط تو داده‌ی نامرئی موتور جستجو/AI اعمال می‌شه، هیچ عددی که کاربر واقعی
            // می‌بینه دست‌نخورده می‌مونه
            ["price"] = Math.Round((product.DiscountPrice ?? product.Price) * 10, 0),
            ["availability"] = product.StockQuantity > 0 ? "https://schema.org/InStock" : "https://schema.org/OutOfStock",
            ["itemCondition"] = "https://schema.org/NewCondition"
        };

        var jsonLd = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = product.Name,
            ["description"] = Truncate(StripHtml(product.Description), 500),
            ["image"] = images,
            ["sku"] = product.Sku,
            ["offers"] = offer
        };
        if (product.Brand is not null)
            jsonLd["brand"] = new Dictionary<string, object?> { ["@type"] = "Brand", ["name"] = product.Brand.Name };
        if (product.Reviews.Count > 0)
            jsonLd["aggregateRating"] = new Dictionary<string, object?>
            {
                ["@type"] = "AggregateRating",
                ["ratingValue"] = Math.Round(product.Reviews.Average(r => r.Rating), 1),
                ["reviewCount"] = product.Reviews.Count
            };

        var html = template;
        html = ReplaceById(html, "title", "page-title", title);
        html = ReplaceMetaContentById(html, "meta-description", description);
        html = ReplaceScriptContentById(html, "jsonld-product", SerializeJsonLd(jsonLd));
        html = InjectHeadExtras(html, BuildCommonHeadTags(title, description, pageUrl, mainImage, "product"));

        return await WriteHtmlAsync(context, html);
    }

    public async Task<bool> TryRenderBlogPostAsync(HttpContext context, string slug)
    {
        var post = await _db.BlogPosts.AsNoTracking()
            .Include(p => p.BlogCategory)
            .Include(p => p.Author)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (post is null) return false;

        var template = await ReadTemplateAsync("blog-post.html");
        if (template is null) return false;

        var baseUrl = GetBaseUrl(context);
        var pageUrl = $"{baseUrl}/blog-post?slug={Uri.EscapeDataString(slug)}";
        var description = Truncate(StripHtml(post.Excerpt) is { Length: > 0 } excerpt ? excerpt : StripHtml(post.Content), 160);
        var authorName = !string.IsNullOrEmpty(post.AuthorName) ? post.AuthorName : $"{post.Author.FirstName} {post.Author.LastName}".Trim();
        var image = AbsoluteUrl(baseUrl, post.FeaturedImageUrl ?? "");
        var title = $"دُرین مارکت | {post.Title}";

        var jsonLd = new Dictionary<string, object?>
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "BlogPosting",
            ["headline"] = post.Title,
            ["image"] = string.IsNullOrWhiteSpace(image) ? null : new[] { image },
            ["datePublished"] = post.PublishedAt,
            ["author"] = new Dictionary<string, object?> { ["@type"] = "Person", ["name"] = authorName },
            ["articleSection"] = post.BlogCategory.Name,
            ["inLanguage"] = "fa-IR",
            ["mainEntityOfPage"] = pageUrl
        };

        var html = template;
        html = ReplaceById(html, "title", "page-title", title);
        html = ReplaceMetaContentById(html, "meta-description", description);
        html = ReplaceScriptContentById(html, "jsonld-post", SerializeJsonLd(jsonLd));
        html = InjectHeadExtras(html, BuildCommonHeadTags(title, description, pageUrl, image, "article"));

        return await WriteHtmlAsync(context, html);
    }

    private async Task<string?> ReadTemplateAsync(string fileName)
    {
        var path = Path.Combine(_env.WebRootPath, fileName);
        if (!File.Exists(path)) return null;
        return await File.ReadAllTextAsync(path);
    }

    private static async Task<bool> WriteHtmlAsync(HttpContext context, string html)
    {
        context.Response.ContentType = "text/html; charset=utf-8";
        await context.Response.WriteAsync(html);
        return true;
    }

    private static string GetBaseUrl(HttpContext context) => $"{context.Request.Scheme}://{context.Request.Host}";

    private static string AbsoluteUrl(string baseUrl, string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return url;
        return baseUrl + (url.StartsWith('/') ? url : "/" + url);
    }

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var noTags = Regex.Replace(html, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(noTags);
        return Regex.Replace(decoded, @"\s+", " ").Trim();
    }

    private static string Truncate(string text, int max) => text.Length <= max ? text : text[..max].TrimEnd() + "…";

    private static string HtmlEncode(string text) => HtmlEncoder.Default.Encode(text);

    private static string SerializeJsonLd(Dictionary<string, object?> data)
    {
        // UnsafeRelaxedJsonEscaping فارسی رو escape نمی‌کنه (خوانا می‌مونه) ولی </>/& رو هم escape
        // نمی‌کنه؛ چون name/headline از دیتابیس میان (مثلاً اسم محصول)، اگه یکی از این مقادیر
        // به‌طور اتفاقی یا عمدی شامل "</script>" باشه، بدون این جایگزینی می‌تونه از تگ script
        // خارج بشه و HTML/اسکریپت دلخواه اجرا کنه
        var options = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        var json = JsonSerializer.Serialize(data, options);
        return json.Replace("<", "\\u003c").Replace(">", "\\u003e").Replace("&", "\\u0026");
    }

    private static string ReplaceById(string html, string tag, string id, string content)
    {
        var pattern = $"<{tag} id=\"{id}\">.*?</{tag}>";
        return Regex.Replace(html, pattern, $"<{tag} id=\"{id}\">{HtmlEncode(content)}</{tag}>", RegexOptions.Singleline);
    }

    private static string ReplaceMetaContentById(string html, string id, string content)
    {
        var pattern = $"(<meta id=\"{id}\"[^>]*content=\")[^\"]*(\"[^>]*/?>)";
        return Regex.Replace(html, pattern, m => m.Groups[1].Value + HtmlEncode(content) + m.Groups[2].Value);
    }

    private static string ReplaceScriptContentById(string html, string id, string jsonContent)
    {
        var pattern = $"(<script type=\"application/ld\\+json\" id=\"{id}\">).*?(</script>)";
        return Regex.Replace(html, pattern, m => m.Groups[1].Value + jsonContent + m.Groups[2].Value, RegexOptions.Singleline);
    }

    private static string BuildCommonHeadTags(string title, string description, string url, string image, string ogType)
    {
        var encTitle = HtmlEncode(title);
        var encDesc = HtmlEncode(description);
        var encUrl = HtmlEncode(url);
        var encImage = HtmlEncode(image);

        return $"""
            <link rel="canonical" href="{encUrl}" />
            <meta property="og:type" content="{ogType}" />
            <meta property="og:title" content="{encTitle}" />
            <meta property="og:description" content="{encDesc}" />
            <meta property="og:url" content="{encUrl}" />
            {(string.IsNullOrEmpty(image) ? "" : $"<meta property=\"og:image\" content=\"{encImage}\" />")}
            <meta property="og:locale" content="fa_IR" />
            <meta name="twitter:card" content="summary_large_image" />
            <meta name="twitter:title" content="{encTitle}" />
            <meta name="twitter:description" content="{encDesc}" />
            """;
    }

    private static string InjectHeadExtras(string html, string extraTags)
    {
        const string marker = "<!--SEO_META_INJECT-->";
        return html.Contains(marker) ? html.Replace(marker, extraTags) : html;
    }
}
