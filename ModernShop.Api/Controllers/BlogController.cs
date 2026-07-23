using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

[ApiController]
[Route("api/blog")]
public class BlogController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BlogController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _db.BlogCategories
            .Select(c => new BlogCategoryDto { Id = c.Id, Name = c.Name, Slug = c.Slug })
            .ToListAsync();

        return Ok(categories);
    }

    // مربوط به گرید و فیلتر دسته‌بندی تو blog.html
    [HttpGet("posts")]
    public async Task<ActionResult<PagedResultDto<BlogPostListItemDto>>> GetPosts(
        [FromQuery] string? category, [FromQuery] int page = 1, [FromQuery] int pageSize = 9)
    {
        var query = _db.BlogPosts.Where(p => p.IsPublished).AsQueryable();

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
            query = query.Where(p => p.BlogCategory.Slug == category);

        query = query.OrderByDescending(p => p.PublishedAt);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new BlogPostListItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = p.Slug,
                Excerpt = p.Excerpt,
                FeaturedImageUrl = p.FeaturedImageUrl,
                CategoryName = p.BlogCategory.Name,
                AuthorName = !string.IsNullOrEmpty(p.AuthorName) ? p.AuthorName : (p.Author.FirstName + " " + p.Author.LastName).Trim(),
                ReadTimeMinutes = p.ReadTimeMinutes,
                PublishedAt = p.PublishedAt
            })
            .ToListAsync();

        return Ok(new PagedResultDto<BlogPostListItemDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    // مربوط به blog-post.html: محتوا، بایو نویسنده، نظرات
    [HttpGet("posts/{slug}")]
    public async Task<ActionResult<BlogPostDetailDto>> GetPost(string slug)
    {
        var post = await _db.BlogPosts
            .Include(p => p.BlogCategory)
            .Include(p => p.Author)
            .Include(p => p.Comments.Where(c => c.IsApproved)).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.IsPublished);

        if (post is null) return NotFound();

        post.ViewCount++;
        await _db.SaveChangesAsync();

        return Ok(new BlogPostDetailDto
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Content = post.Content,
            FeaturedImageUrl = post.FeaturedImageUrl,
            CategoryName = post.BlogCategory.Name,
            AuthorName = !string.IsNullOrEmpty(post.AuthorName) ? post.AuthorName : $"{post.Author.FirstName} {post.Author.LastName}".Trim(),
            AuthorBio = post.Author.Bio,
            ReadTimeMinutes = post.ReadTimeMinutes,
            PublishedAt = post.PublishedAt,
            ViewCount = post.ViewCount,
            Comments = post.Comments
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new BlogCommentDto
                {
                    Id = c.Id,
                    UserFullName = $"{c.User.FirstName} {c.User.LastName}".Trim(),
                    Content = c.Content,
                    CreatedAt = c.CreatedAt
                })
                .ToList()
        });
    }

    // مربوط به فرم «دیدگاه خود را بنویسید» تو blog-post.html
    [HttpPost("comments")]
    [Authorize]
    public async Task<IActionResult> AddComment([FromBody] CreateCommentRequestDto request)
    {
        var postExists = await _db.BlogPosts.AnyAsync(p => p.Id == request.BlogPostId);
        if (!postExists) return NotFound(new { message = "مطلب یافت نشد" });

        _db.BlogComments.Add(new BlogComment
        {
            BlogPostId = request.BlogPostId,
            UserId = _currentUser.UserId!.Value,
            Content = request.Content,
            IsApproved = false
        });
        await _db.SaveChangesAsync();

        return Ok(new { message = "دیدگاه شما ثبت شد و پس از تایید نمایش داده می‌شود" });
    }
}
