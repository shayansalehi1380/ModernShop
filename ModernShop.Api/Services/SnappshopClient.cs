using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ModernShop.Api.Services;

public class SnappshopClient
{
    private readonly HttpClient _http;
    private readonly ILogger<SnappshopClient> _log;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public SnappshopClient(HttpClient http, ILogger<SnappshopClient> log)
    {
        _http = http;
        _log = log;
    }

    // GET /vendors
    public async Task<List<SnappshopVendor>> GetVendorsAsync(CancellationToken ct = default)
    {
        var r = await SendAsync<List<SnappshopVendor>>(HttpMethod.Get, "vendors", null, ct);
        return r.Data ?? new();
    }

    // GET /vendors/{id}
    public async Task<SnappshopVendor?> GetVendorAsync(string vendorId, CancellationToken ct = default)
        => (await SendAsync<SnappshopVendor>(HttpMethod.Get, $"vendors/{vendorId}", null, ct)).Data;

    // GET /vendors/{id}/products?page=N  (۲۰ تا در هر صفحه)
    public async Task<SnappshopEnvelope<List<SnappshopProduct>>> GetProductsPageAsync(
        string vendorId, int page, CancellationToken ct = default)
        => await SendAsync<List<SnappshopProduct>>(HttpMethod.Get, $"vendors/{vendorId}/products?page={page}", null, ct);

    // همه صفحه‌ها
    public async IAsyncEnumerable<SnappshopProduct> GetAllProductsAsync(
        string vendorId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var page = 1;
        int totalPages;
        do
        {
            var res = await GetProductsPageAsync(vendorId, page, ct);
            foreach (var p in res.Data ?? new()) yield return p;
            totalPages = res.Meta?.Pagination?.TotalPages ?? 1;
            page++;
        } while (page <= totalPages);
    }

    // GET /vendors/{id}/products/{productId}
    public async Task<SnappshopProduct?> GetProductAsync(string vendorId, string productId, CancellationToken ct = default)
        => (await SendAsync<SnappshopProduct>(HttpMethod.Get, $"vendors/{vendorId}/products/{productId}", null, ct)).Data;

    // PATCH /vendors/{id}/products  (حداکثر ۵۰ محصول)
    public async Task<List<SnappshopUpdateResult>> UpdateProductsAsync(
        string vendorId, IReadOnlyCollection<SnappshopUpdateItem> items, CancellationToken ct = default)
    {
        if (items.Count == 0) return new();
        if (items.Count > 50)
            throw new ArgumentException("حداکثر ۵۰ محصول در هر درخواست مجاز است.", nameof(items));

        var res = await SendAsync<List<SnappshopUpdateResult>>(
            HttpMethod.Patch, $"vendors/{vendorId}/products", new { products = items }, ct);
        return res.Data ?? new();
    }

    private async Task<SnappshopEnvelope<T>> SendAsync<T>(
        HttpMethod method, string url, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, url);
        if (body != null) req.Content = JsonContent.Create(body, options: Json);

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            int? code = null; string msg = raw;
            try
            {
                var err = JsonSerializer.Deserialize<SnappshopEnvelope<object>>(raw, Json);
                code = err?.Code; msg = err?.Message ?? raw;
            }
            catch { /* پاسخ JSON نبود */ }

            if (resp.StatusCode == HttpStatusCode.Unauthorized)
                msg = $"توکن اسنپ‌شاپ نامعتبر یا منقضی است (کد {code}). توکن جدید بسازید. {msg}";

            _log.LogWarning("Snappshop {Method} {Url} -> {Status}: {Msg}", method, url, (int)resp.StatusCode, msg);
            throw new SnappshopApiException(msg, (int)resp.StatusCode, code);
        }

        return JsonSerializer.Deserialize<SnappshopEnvelope<T>>(raw, Json)
               ?? throw new SnappshopApiException("پاسخ خالی از اسنپ‌شاپ", (int)resp.StatusCode);
    }
}