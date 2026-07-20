using System.Security.Claims;
using ModernShop.Core.Interfaces;

namespace ModernShop.Api.Services;

/// <summary>
/// پیاده‌سازی ICurrentUserService (تعریف‌شده در Atelier.Core) بر اساس claim های JWT.
/// این تنها جایی‌یه که به HttpContext وابسته‌ست؛ بقیه پروژه فقط از اینترفیس استفاده می‌کنه.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public int? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    public string? PhoneNumber =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.MobilePhone);

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
