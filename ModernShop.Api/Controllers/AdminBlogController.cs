using ModernShop.Api.Services;
using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «وبلاگ» تو پنل مدیریت (admin.html)
[ApiController]
[Route("api/admin/blog")]
[Authorize(Policy = "AdminOnly")]
public class AdminBlogController : ControllerBase
{
    // چون ادمین یک کاربر واقعی تو جدول Users نیست ولی BlogPost.AuthorId اجباریه،
    // پست‌های ساخته‌شده از پنل مدیریت به این کاربر سیستمی نسبت داده می‌شن.
    private const string SystemAuthorPhone = "00000000000";

    private readonly AppDbContext _db;

    public AdminBlogController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminBlogPostListItemDto>>> GetAll()
    {
        var posts = await _db.BlogPosts
            .Include(p => p.BlogCategory)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new AdminBlogPostListItemDto
            {
                Id = p.Id,
                Title = p.Title,
                CategoryName = p.BlogCategory.Name,
                ReadTimeMinutes = p.ReadTimeMinutes,
                IsPublished = p.IsPublished,
                PublishedAt = p.PublishedAt,
                ViewCount = p.ViewCount
            })
            .ToListAsync();

        return Ok(posts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminBlogPostDetailDto>> GetById(int id)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return NotFound();

        return Ok(MapToDetailDto(post));
    }

    [HttpPost]
    public async Task<ActionResult<AdminBlogPostDetailDto>> Create([FromBody] SaveBlogPostRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "عنوان مطلب الزامی است" });

        var categoryExists = await _db.BlogCategories.AnyAsync(c => c.Id == request.BlogCategoryId);
        if (!categoryExists) return BadRequest(new { message = "دسته‌بندی معتبر نیست" });

        var authorId = await GetOrCreateSystemAuthorIdAsync();

        var post = new BlogPost
        {
            Title = request.Title.Trim(),
            Slug = await ResolveSlugAsync(request.Slug, request.Title, null),
            Excerpt = string.IsNullOrWhiteSpace(request.Excerpt) ? null : request.Excerpt.Trim(),
            Content = request.Content.Trim(),
            FeaturedImageUrl = string.IsNullOrWhiteSpace(request.FeaturedImageUrl) ? null : request.FeaturedImageUrl.Trim(),
            BlogCategoryId = request.BlogCategoryId,
            AuthorId = authorId,
            AuthorName = string.IsNullOrWhiteSpace(request.AuthorName) ? null : request.AuthorName.Trim(),
            ReadTimeMinutes = request.ReadTimeMinutes,
            IsPublished = request.IsPublished,
            PublishedAt = request.IsPublished ? DateTime.UtcNow : null
        };

        _db.BlogPosts.Add(post);
        await _db.SaveChangesAsync();

        return Ok(MapToDetailDto(post));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminBlogPostDetailDto>> Update(int id, [FromBody] SaveBlogPostRequestDto request)
    {
        var post = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id);
        if (post is null) return NotFound();

        if (string.IsNullOrWhiteSpace(request.Title))
            return BadRequest(new { message = "عنوان مطلب الزامی است" });

        var categoryExists = await _db.BlogCategories.AnyAsync(c => c.Id == request.BlogCategoryId);
        if (!categoryExists) return BadRequest(new { message = "دسته‌بندی معتبر نیست" });

        post.Title = request.Title.Trim();
        post.Slug = await ResolveSlugAsync(request.Slug, request.Title, post.Id);
        post.Excerpt = string.IsNullOrWhiteSpace(request.Excerpt) ? null : request.Excerpt.Trim();
        post.Content = request.Content.Trim();
        post.FeaturedImageUrl = string.IsNullOrWhiteSpace(request.FeaturedImageUrl) ? null : request.FeaturedImageUrl.Trim();
        post.BlogCategoryId = request.BlogCategoryId;
        post.AuthorName = string.IsNullOrWhiteSpace(request.AuthorName) ? null : request.AuthorName.Trim();
        post.ReadTimeMinutes = request.ReadTimeMinutes;

        // اگه همین الان منتشر می‌شه و قبلا تاریخ انتشار نداشته، الان رو ثبت کن؛
        // اگه از حالت منتشرشده به پیش‌نویس برگرده، تاریخ قبلی رو دست نمی‌زنیم (طبق منطق پنل)
        if (request.IsPublished && post.PublishedAt is null)
            post.PublishedAt = DateTime.UtcNow;
        post.IsPublished = request.IsPublished;

        await _db.SaveChangesAsync();

        return Ok(MapToDetailDto(post));
    }

    // برخلاف محصول، حذف پست وبلاگ حذف واقعیه؛ چون تنها جدول وابسته (BlogComment) با
    // DeleteBehavior.Cascade تعریف شده و پاک شدنش امن است.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _db.BlogPosts.FindAsync(id);
        if (post is null) return NotFound();

        _db.BlogPosts.Remove(post);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    private static AdminBlogPostDetailDto MapToDetailDto(BlogPost post) => new()
    {
        Id = post.Id,
        Title = post.Title,
        Slug = post.Slug,
        Excerpt = post.Excerpt,
        Content = post.Content,
        FeaturedImageUrl = post.FeaturedImageUrl,
        BlogCategoryId = post.BlogCategoryId,
        AuthorName = post.AuthorName,
        ReadTimeMinutes = post.ReadTimeMinutes,
        IsPublished = post.IsPublished
    };

    private async Task<int> GetOrCreateSystemAuthorIdAsync()
    {
        var existing = await _db.Users.FirstOrDefaultAsync(u => u.PhoneNumber == SystemAuthorPhone);
        if (existing is not null) return existing.Id;

        var systemAuthor = new User
        {
            PhoneNumber = SystemAuthorPhone,
            FirstName = "مدیر",
            LastName = "فروشگاه",
            Bio = "نویسنده مطالب منتشرشده از پنل مدیریت آتلیه",
            IsPhoneVerified = true,
            IsActive = true
        };
        _db.Users.Add(systemAuthor);
        await _db.SaveChangesAsync();

        return systemAuthor.Id;
    }

    private async Task<string> ResolveSlugAsync(string? requestedSlug, string title, int? excludingPostId)
    {
        var baseSlug = SlugHelper.Slugify(string.IsNullOrWhiteSpace(requestedSlug) ? title : requestedSlug);
        if (string.IsNullOrWhiteSpace(baseSlug)) baseSlug = "post";

        var slug = baseSlug;
        var suffix = 2;
        while (await _db.BlogPosts.AnyAsync(p => p.Slug == slug && p.Id != (excludingPostId ?? 0)))
        {
            slug = $"{baseSlug}-{suffix++}";
        }

        return slug;
    }
}
