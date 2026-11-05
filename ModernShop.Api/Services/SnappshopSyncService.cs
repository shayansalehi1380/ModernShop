using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ModernShop.Core.Entities;
using ModernShop.Infrastructure.Data;

namespace ModernShop.Api.Services;

public class SnappshopSyncReport
{
    public int Total { get; set; }
    public int Succeeded { get; set; }
    public List<string> Failures { get; } = new();
}

public class SnappshopSyncService
{
    private readonly AppDbContext _db;
    private readonly SnappshopClient _client;
    private readonly SnappshopSettings _s;
    private readonly ILogger<SnappshopSyncService> _log;

    public SnappshopSyncService(AppDbContext db, SnappshopClient client,
        IOptions<SnappshopSettings> s, ILogger<SnappshopSyncService> log)
    {
        _db = db;
        _client = client;
        _s = s.Value;
        _log = log;
    }

    public async Task<SnappshopSyncReport> SyncAllAsync(CancellationToken ct = default)
    {
        var report = new SnappshopSyncReport();
        if (!_s.Enabled || string.IsNullOrWhiteSpace(_s.VendorId)) return report;

        var products = await _db.Products
            .AsNoTracking()
            .Where(p => p.SnappshopSku != null && p.SnappshopSku != "")
            .ToListAsync(ct);

        report.Total = products.Count;
        var batchSize = Math.Clamp(_s.BatchSize, 1, 50);

        foreach (var chunk in products.Chunk(batchSize))
        {
            var items = chunk.Select(MapToUpdate).ToList();
            try
            {
                var results = await _client.UpdateProductsAsync(_s.VendorId, items, ct);
                foreach (var r in results)
                {
                    if (r.Status) report.Succeeded++;
                    else report.Failures.Add($"{r.Sku ?? r.Id}: {string.Join(" | ", r.Messages)}");
                }
            }
            catch (SnappshopApiException ex)
            {
                _log.LogError(ex, "Snappshop batch failed");
                report.Failures.Add($"batch error: {ex.Message}");
                if (ex.HttpStatus == 401) break; // توکن نامعتبر؛ ادامه بی‌فایده است
            }
        }

        _log.LogInformation("Snappshop sync: {Ok}/{Total} ok, {Fail} failures",
            report.Succeeded, report.Total, report.Failures.Count);
        return report;
    }

    public async Task<SnappshopSyncReport> SyncOneAsync(int productId, CancellationToken ct = default)
    {
        var report = new SnappshopSyncReport { Total = 1 };

        var p = await _db.Products.AsNoTracking().FirstOrDefaultAsync(x => x.Id == productId, ct);
        if (p == null || string.IsNullOrWhiteSpace(p.SnappshopSku))
        {
            report.Failures.Add("محصول پیدا نشد یا SKU اسنپ‌شاپ ندارد.");
            return report;
        }

        var res = await _client.UpdateProductsAsync(_s.VendorId, new[] { MapToUpdate(p) }, ct);
        foreach (var r in res)
        {
            if (r.Status) report.Succeeded++;
            else report.Failures.Add(string.Join(" | ", r.Messages));
        }
        return report;
    }

    private static SnappshopUpdateItem MapToUpdate(Product p)
    {
        // محصول غیرفعال → موجودی صفر تا در اسنپ‌شاپ ناموجود شود
        var stock = p.IsActive ? Math.Max(0, p.StockQuantity) : 0;

        var item = new SnappshopUpdateItem
        {
            Sku = p.SnappshopSku,
            Stock = stock,
            Price = (long)p.Price              // تومان
        };

        // تخفیف فقط وقتی معتبر است که کمتر از قیمت اصلی باشد
        if (p.DiscountPrice is > 0 && p.DiscountPrice < p.Price)
        {
            var today = DateTime.UtcNow.Date;
            item.SpecialPrice = (long)p.DiscountPrice.Value;
            item.SpecialPriceStartAt = today.ToString("yyyy-MM-dd");
            item.SpecialPriceEndAt = today.AddDays(30).ToString("yyyy-MM-dd");
            item.SpecialPriceStock = stock;
        }

        return item;
    }
}