using System.Security.Cryptography;
using System.Text;
using ModernShop.Api.Services;
using ModernShop.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ModernShop.Api.Controllers;

// مربوط به صفحه ورود پنل مدیریت
[ApiController]
[Route("api/admin/auth")]
public class AdminAuthController : ControllerBase
{
    private readonly AdminSettings _settings;
    private readonly JwtTokenService _jwtTokenService;

    public AdminAuthController(IOptions<AdminSettings> settings, JwtTokenService jwtTokenService)
    {
        _settings = settings.Value;
        _jwtTokenService = jwtTokenService;
    }

    // EnableRateLimiting جلوی brute-force رو با محدود کردن تعداد تلاش هر IP می‌گیره (پالیسی تو Program.cs)
    [HttpPost("login")]
    [EnableRateLimiting("AdminLogin")]
    public IActionResult Login([FromBody] AdminLoginRequestDto request)
    {
        if (!FixedTimeEquals(request.Username, _settings.Username) || !FixedTimeEquals(request.Password, _settings.Password))
            return BadRequest(new { message = "نام کاربری یا رمز عبور اشتباه است" });

        var token = _jwtTokenService.GenerateAdminToken();
        return Ok(new { token });
    }

    // مقایسه‌ی معمولی رشته‌ها (==) زودتر برمی‌گرده وقتی اولین کاراکتر متفاوت پیدا بشه؛ این تفاوت
    // زمانی (هرچند خیلی کوچیک) تئوریاً قابل اندازه‌گیریه. FixedTimeEquals همیشه دقیقاً یک زمان
    // طول می‌کشه، صرف‌نظر از اینکه رشته کجا فرق می‌کنه.
    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
}
