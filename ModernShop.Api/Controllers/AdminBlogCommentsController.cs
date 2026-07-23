using ModernShop.Core.DTOs;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «نظرات» تو پنل مدیریت (admin.html) — تایید/رد نظرات وبلاگ
[ApiController]
[Route("api/admin/blog-comments")]
[Authorize(Policy = "AdminOnly")]
public class AdminBlogCommentsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminBlogCommentsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminBlogCommentListItemDto>>> GetAll([FromQuery] bool? approved)
    {
        var query = _db.BlogComments
            .Include(c => c.BlogPost)
            .Include(c => c.User)
            .AsQueryable();

        if (approved.HasValue)
            query = query.Where(c => c.IsApproved == approved.Value);

        var comments = await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new AdminBlogCommentListItemDto
            {
                Id = c.Id,
                BlogPostId = c.BlogPostId,
                BlogPostTitle = c.BlogPost.Title,
                BlogPostSlug = c.BlogPost.Slug,
                UserFullName = $"{c.User.FirstName} {c.User.LastName}".Trim(),
                Content = c.Content,
                IsApproved = c.IsApproved,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return Ok(comments);
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var comment = await _db.BlogComments.FindAsync(id);
        if (comment == null) return NotFound(new { message = "نظر یافت نشد" });

        comment.IsApproved = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var comment = await _db.BlogComments.FindAsync(id);
        if (comment == null) return NotFound(new { message = "نظر یافت نشد" });

        comment.IsApproved = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
