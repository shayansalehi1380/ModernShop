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

        // ۱ دقیقه و ۳۰ ثانیه فرصت برای وارد کردن کد؛ همینو تو verify-otp هم چک می‌کنیم
        const int otpExpirySeconds = 90;

        _db.OtpCodes.Add(new OtpCode
        {
            PhoneNumber = request.PhoneNumber,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddSeconds(otpExpirySeconds),
            IsUsed = false
        });
        await _db.SaveChangesAsync();

        await _smsService.SendOtpAsync(request.PhoneNumber, code);

        return Ok(new { message = "کد تایید ارسال شد", expiresInSeconds = otpExpirySeconds });
    }

    // مرحله ۲ از auth.html: کد ۵ رقمی
    [HttpPost("verify-otp")]
    public async Task<ActionResult<AuthResponseDto>> VerifyOtp([FromBody] VerifyOtpRequestDto request)
    {
        var otp = await _db.OtpCodes
            .Where(x => x.PhoneNumber == request.PhoneNumber && !x.IsUsed)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp is null || otp.Code != request.Code)
            return BadRequest(new { message = "کد تایید اشتباه است" });

        if (otp.ExpiresAt < DateTime.UtcNow)
            return BadRequest(new { message = "زمان وارد کردن کد به پایان رسیده، دوباره درخواست کد کنید" });

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

        await MergeGuestCartAsync(user.Id);

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

    // بعد از لاگین، سبد خرید مهمان (که با X-Guest-Session-Id ردیابی می‌شد) رو با سبد
    // کاربر لاگین‌شده یکی می‌کنه؛ وگرنه کاربر بعد از لاگین سبدش رو خالی می‌دید (چون
    // apiFetch بعد از لاگین دیگه هدر مهمان رو نمی‌فرسته و یه سبد کاملا جدید ساخته می‌شه).
    private async Task MergeGuestCartAsync(int userId)
    {
        var guestSessionId = Request.Headers["X-Guest-Session-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(guestSessionId)) return;

        var guestCart = await _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.GuestSessionId == guestSessionId);
        if (guestCart is null || guestCart.Items.Count == 0) return;

        var userCart = await _db.Carts.Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (userCart is null)
        {
            userCart = new Cart { UserId = userId };
            _db.Carts.Add(userCart);
        }

        foreach (var guestItem in guestCart.Items)
        {
            var existing = userCart.Items.FirstOrDefault(i =>
                i.ProductId == guestItem.ProductId && i.ProductVariantId == guestItem.ProductVariantId);

            if (existing is not null)
            {
                existing.Quantity += guestItem.Quantity;
            }
            else
            {
                userCart.Items.Add(new CartItem
                {
                    ProductId = guestItem.ProductId,
                    ProductVariantId = guestItem.ProductVariantId,
                    Quantity = guestItem.Quantity,
                    UnitPrice = guestItem.UnitPrice
                });
            }
        }

        _db.Carts.Remove(guestCart);
        await _db.SaveChangesAsync();
    }
}
