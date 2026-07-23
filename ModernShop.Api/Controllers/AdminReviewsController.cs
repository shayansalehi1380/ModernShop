using ModernShop.Core.DTOs;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Api.Controllers;

// مربوط به بخش «نظرات» تو پنل مدیریت (admin.html) — تایید/رد و پاسخ به نظرات مشتریان
[ApiController]
[Route("api/admin/reviews")]
[Authorize(Policy = "AdminOnly")]
public class AdminReviewsController : ControllerBase
{
    private readonly AppDbContext _db;

    public AdminReviewsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<AdminReviewListItemDto>>> GetAll([FromQuery] bool? approved)
    {
        var query = _db.ProductReviews
            .Include(r => r.Product)
            .Include(r => r.User)
            .AsQueryable();

        if (approved.HasValue)
            query = query.Where(r => r.IsApproved == approved.Value);

        var reviews = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new AdminReviewListItemDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                ProductName = r.Product.Name,
                ProductSlug = r.Product.Slug,
                UserFullName = $"{r.User.FirstName} {r.User.LastName}".Trim(),
                Rating = r.Rating,
                Comment = r.Comment,
                IsApproved = r.IsApproved,
                AdminReply = r.AdminReply,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return Ok(reviews);
    }

    [HttpPut("{id}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null) return NotFound(new { message = "نظر یافت نشد" });

        review.IsApproved = true;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null) return NotFound(new { message = "نظر یافت نشد" });

        review.IsApproved = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPut("{id}/reply")]
    public async Task<IActionResult> Reply(int id, [FromBody] ReplyReviewRequestDto request)
    {
        var review = await _db.ProductReviews.FindAsync(id);
        if (review == null) return NotFound(new { message = "نظر یافت نشد" });

        review.AdminReply = string.IsNullOrWhiteSpace(request.Reply) ? null : request.Reply.Trim();
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
