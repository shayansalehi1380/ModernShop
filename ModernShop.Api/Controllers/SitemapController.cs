using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModernShop.Infrastructure.Data;

namespace Atelier.Api.Controllers;

// نقشه‌ی سایت پویا (نه فایل استاتیک) چون باید همیشه لیست به‌روز محصولات و مطالب وبلاگ رو
// نشون بده. آدرس پایه (دامنه) هم از روی خود درخواست ساخته می‌شه، پس نیازی به تنظیم دستی
// دامنه تو هیچ فایلی نیست.
[ApiController]
public class SitemapController : ControllerBase
{
    private readonly AppDbContext _db;
    public SitemapController(AppDbContext db) => _db = db;

    [HttpGet("/sitemap.xml")]
    public async Task<IActionResult> GetSitemap()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";

        var urls = new List<(string Loc, DateTime? LastMod, string ChangeFreq, string Priority)>
        {
            ($"{baseUrl}/home", null, "daily", "1.0"),
            ($"{baseUrl}/shop", null, "daily", "0.9"),
            ($"{baseUrl}/blog", null, "daily", "0.7"),
        };

        var products = await _db.Products.AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.Slug, p.CreatedAt })
            .ToListAsync();
        urls.AddRange(products.Select(p => ($"{baseUrl}/product?slug={Uri.EscapeDataString(p.Slug)}", (DateTime?)p.CreatedAt, "weekly", "0.8")));

        var posts = await _db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished)
            .Select(p => new { p.Slug, p.PublishedAt })
            .ToListAsync();
        urls.AddRange(posts.Select(p => ($"{baseUrl}/blog-post?slug={Uri.EscapeDataString(p.Slug)}", p.PublishedAt, "monthly", "0.6")));

        var staticPages = await _db.StaticPages.AsNoTracking()
            .Select(p => p.Slug)
            .ToListAsync();
        urls.AddRange(staticPages.Select(slug => ($"{baseUrl}/page?slug={Uri.EscapeDataString(slug)}", (DateTime?)null, "monthly", "0.4")));

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var (loc, lastMod, changeFreq, priority) in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{System.Net.WebUtility.HtmlEncode(loc)}</loc>");
            if (lastMod.HasValue)
                sb.AppendLine($"    <lastmod>{lastMod.Value:yyyy-MM-dd}</lastmod>");
            sb.AppendLine($"    <changefreq>{changeFreq}</changefreq>");
            sb.AppendLine($"    <priority>{priority}</priority>");
            sb.AppendLine("  </url>");
        }
        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml", Encoding.UTF8);
    }
}
