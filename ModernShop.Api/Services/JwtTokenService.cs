using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ModernShop.Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ModernShop.Api.Services;

public class JwtSettings
{
    public string Key { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryMinutes { get; set; } = 10080; // پیش‌فرض ۷ روز
}

public class JwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.MobilePhone, user.PhoneNumber)
        };

        return WriteToken(claims);
    }

    /// <summary>
    /// توکن مخصوص پنل مدیریت (admin.html). چون ادمین یک کاربر واقعی تو جدول Users نیست،
    /// این توکن هیچ NameIdentifier ای نداره؛ فقط با claim اختصاصی "scope"="admin" مشخص می‌شه
    /// و AdminOnly policy تو Program.cs دقیقاً همین رو چک می‌کنه.
    /// </summary>
    public string GenerateAdminToken()
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, "admin"),
            new("scope", "admin")
        };

        return WriteToken(claims);
    }

    private string WriteToken(List<Claim> claims)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
