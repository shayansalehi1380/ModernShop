using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

// مربوط به فرم عضویت در خبرنامه (پایین صفحه اصلی و وبلاگ)
[ApiController]
[Route("api/newsletter")]
public class NewsletterController : ControllerBase
{
    private readonly AppDbContext _db;
    public NewsletterController(AppDbContext db) => _db = db;

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return BadRequest(new { message = "آدرس ایمیل معتبر نیست" });

        var exists = await _db.NewsletterSubscribers.AnyAsync(s => s.Email == request.Email);
        if (exists) return Ok(new { message = "قبلاً عضو شده‌اید" });

        _db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = request.Email });
        await _db.SaveChangesAsync();

        return Ok(new { message = "با موفقیت در خبرنامه عضو شدید" });
    }
}

// این یکی چون فقط مخصوص همین درخواسته و جای دیگه‌ای استفاده نمی‌شه، تو Core تعریف نشده
public class SubscribeRequestDto
{
    public string Email { get; set; } = null!;
}
