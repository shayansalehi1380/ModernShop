using System.Text.RegularExpressions;
using ModernShop.Api.Services;
using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;
    private readonly JwtTokenService _jwtTokenService;

    public AuthController(AppDbContext db, ISmsService smsService, JwtTokenService jwtTokenService)
    {
        _db = db;
        _smsService = smsService;
        _jwtTokenService = jwtTokenService;
    }

    // مرحله ۱ از auth.html: فقط شماره موبایل
    [HttpPost("send-otp")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber) || !Regex.IsMatch(request.PhoneNumber, @"^09\d{9}$"))
            return BadRequest(new { message = "شماره موبایل معتبر نیست" });

        var code = Random.Shared.Next(10000, 99999).ToString();

        _db.OtpCodes.Add(new OtpCode
        {
            PhoneNumber = request.PhoneNumber,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(2),
            IsUsed = false
        });
        await _db.SaveChangesAsync();

        await _smsService.SendOtpAsync(request.PhoneNumber, code);

        return Ok(new { message = "کد تایید ارسال شد" });
    }

    // مرحله ۲ از auth.html: کد ۵ رقمی
    [HttpPost("verify-otp")]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var otp = await _db.OtpCodes
            .Where(x => x.PhoneNumber == request.PhoneNumber && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp is null || otp.Code != request.Code || otp.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "کد تایید نامعتبر یا منقضی‌شده است" });

        otp.IsUsed = true;

        var user = await _db.Users.FirstOrDefaultAsync(x => x.PhoneNumber == request.PhoneNumber);
        var isNewUser = user is null;

        if (user is null)
        {
            user = new User { PhoneNumber = request.PhoneNumber, IsPhoneVerified = true };
            _db.Users.Add(user);
        }
        else
        {
            user.IsPhoneVerified = true;
        }

        await _db.SaveChangesAsync();

        var token = _jwtTokenService.GenerateToken(user);

        return Ok(new AuthResponseDto
        {
            Token = token,
            UserId = user.Id,
            PhoneNumber = user.PhoneNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsNewUser = isNewUser
        });
    }
}
