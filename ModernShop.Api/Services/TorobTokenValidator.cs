using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Security;

namespace ModernShop.Api.Services;

/// <summary>
/// اعتبارسنجی توکن JWT امضاشده با EdDSA (Ed25519) که ترب تو هدر X-Torob-Token می‌فرسته،
/// طبق «راهنمای توکن API ترب». چون این الگوریتم امضا تو کتابخانه‌ی JWT مایکروسافت پشتیبانی
/// نمی‌شه، خود امضا رو مستقیم با BouncyCastle چک می‌کنیم (نه از طریق pipeline استاندارد
/// [Authorize] سایت که مخصوص توکن‌های خودمونه).
/// </summary>
public class TorobTokenValidator
{
    private readonly Ed25519PublicKeyParameters? _publicKey;
    private readonly string? _configError;
    private readonly string _expectedAudience;

    public TorobTokenValidator(IOptions<TorobSettings> settings)
    {
        _expectedAudience = settings.Value.ExpectedAudience ?? "";

        try
        {
            var derBytes = ParsePemBody(settings.Value.PublicKeyPem ?? "");
            _publicKey = PublicKeyFactory.CreateKey(derBytes) as Ed25519PublicKeyParameters;
            if (_publicKey is null)
                _configError = "کلید عمومی ترب (Torob:PublicKeyPem) در appsettings.json تنظیم یا معتبر نیست";
        }
        catch (Exception ex)
        {
            _configError = $"خطا در خواندن کلید عمومی ترب: {ex.Message}";
        }
    }

    public bool Validate(string? token, out string error)
    {
        error = "";

        if (_configError is not null) { error = _configError; return false; }
        if (string.IsNullOrWhiteSpace(token)) { error = "missing X-Torob-Token header"; return false; }

        var parts = token.Split('.');
        if (parts.Length != 3) { error = "invalid token format"; return false; }

        byte[] payloadBytes, signatureBytes;
        try
        {
            payloadBytes = Base64UrlDecode(parts[1]);
            signatureBytes = Base64UrlDecode(parts[2]);
        }
        catch
        {
            error = "invalid token encoding";
            return false;
        }

        var signingInput = Encoding.UTF8.GetBytes($"{parts[0]}.{parts[1]}");
        var signer = new Ed25519Signer();
        signer.Init(false, _publicKey!);
        signer.BlockUpdate(signingInput, 0, signingInput.Length);
        if (!signer.VerifySignature(signatureBytes))
        {
            error = "invalid token signature";
            return false;
        }

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(payloadBytes);
        }
        catch
        {
            error = "invalid token payload";
            return false;
        }

        using (doc)
        {
            var root = doc.RootElement;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            if (root.TryGetProperty("exp", out var expEl) && expEl.TryGetInt64(out var exp) && exp < now)
            {
                error = "token expired";
                return false;
            }
            if (root.TryGetProperty("nbf", out var nbfEl) && nbfEl.TryGetInt64(out var nbf) && nbf > now)
            {
                error = "token not yet valid";
                return false;
            }
            if (!root.TryGetProperty("aud", out var audEl) || audEl.GetString() != _expectedAudience)
            {
                error = "audience mismatch";
                return false;
            }
        }

        return true;
    }

    // فایل appsettings.json فقط شامل بدنه‌ی PEM هست (بین BEGIN/END PUBLIC KEY)؛ همون رو
    // base64-decode می‌کنیم تا بایت‌های خام DER (فرمت SubjectPublicKeyInfo) به دست بیاد
    private static byte[] ParsePemBody(string pem)
    {
        var body = pem
            .Replace("-----BEGIN PUBLIC KEY-----", "")
            .Replace("-----END PUBLIC KEY-----", "")
            .Replace("\r", "")
            .Replace("\n", "")
            .Trim();
        return Convert.FromBase64String(body);
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
