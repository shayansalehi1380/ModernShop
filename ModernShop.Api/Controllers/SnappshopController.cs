using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ModernShop.Api.Services;

namespace ModernShop.Api.Controllers;

[ApiController]
[Route("api/admin/snappshop")]
[Authorize] // ← مثل بقیه کنترلرهای ادمین (Role/Policy خودتان)
public class SnappshopController : ControllerBase
{
    private readonly SnappshopClient _client;
    private readonly SnappshopSyncService _sync;
    private readonly SnappshopSettings _s;

    public SnappshopController(SnappshopClient client, SnappshopSyncService sync, IOptions<SnappshopSettings> s)
    { _client = client; _sync = sync; _s = s.Value; }

    [HttpGet("vendors")]
    public async Task<IActionResult> Vendors(CancellationToken ct) => Ok(await _client.GetVendorsAsync(ct));

    [HttpGet("products")]
    public async Task<IActionResult> Products([FromQuery] int page = 1, CancellationToken ct = default)
        => Ok(await _client.GetProductsPageAsync(_s.VendorId, page, ct));

    [HttpGet("products/{id}")]
    public async Task<IActionResult> Product(string id, CancellationToken ct)
        => Ok(await _client.GetProductAsync(_s.VendorId, id, ct));

    [HttpPost("sync")]
    public async Task<IActionResult> SyncAll(CancellationToken ct) => Ok(await _sync.SyncAllAsync(ct));

    [HttpPost("sync/{productId:int}")]
    public async Task<IActionResult> SyncOne(int productId, CancellationToken ct)
        => Ok(await _sync.SyncOneAsync(productId, ct));
}