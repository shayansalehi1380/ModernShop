using ModernShop.Api.Services;
using ModernShop.Core.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ModernShop.Api.Controllers;

// مربوط به صفحه ورود پنل مدیریت (admin.html)
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

    [HttpPost("login")]
    public IActionResult Login([FromBody] AdminLoginRequestDto request)
    {
        if (request.Username != _settings.Username || request.Password != _settings.Password)
            return BadRequest(new { message = "نام کاربری یا رمز عبور اشتباه است" });

        var token = _jwtTokenService.GenerateAdminToken();
        return Ok(new { token });
    }
}
