using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Core.Enums;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

// عمداً [Authorize] روی کل کنترلر نیست، چون سبد خرید باید برای کاربر مهمان هم کار کنه.
// کاربر مهمان یک GuestSessionId (یک GUID که خودش تو مرورگر می‌سازه) رو تو هدر X-Guest-Session-Id می‌فرسته.
[ApiController]
[Route("api/cart")]
public class CartController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public CartController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<CartDto>> GetCart()
    {
        var cart = await GetOrCreateCartAsync();
        var (shippingCost, threshold) = await GetShippingSettingsAsync();
        return Ok(ToDto(cart, shippingCost, threshold, 0, null));
    }

    // مربوط به دکمه «افزودن به سبد» روی کارت محصول
    [HttpPost("items")]
    public async Task<ActionResult<CartDto>> AddItem([FromBody] AddToCartRequestDto request)
    {
        var product = await _db.Products.FindAsync(request.ProductId);
        if (product is null) return NotFound(new { message = "محصول یافت نشد" });

        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = await _db.ProductVariants.FindAsync(request.ProductVariantId.Value);
            if (variant is null) return NotFound(new { message = "تنوع انتخاب‌شده یافت نشد" });
        }
        var availableStock = variant?.StockQuantity ?? product.StockQuantity;

        var cart = await GetOrCreateCartAsync();

        var existing = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId && i.ProductVariantId == request.ProductVariantId);

        var requestedTotal = (existing?.Quantity ?? 0) + request.Quantity;
        if (requestedTotal > availableStock)
        {
            return BadRequest(new
            {
                message = availableStock <= 0
                    ? "این محصول موجود نیست"
                    : $"تنها {availableStock} عدد از این محصول موجود است"
            });
        }

        if (existing is not null)
        {
            existing.Quantity = requestedTotal;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity,
                UnitPrice = (variant?.PriceAdjustment ?? 0) + (product.DiscountPrice ?? product.Price)
            });
        }

        await _db.SaveChangesAsync();

        var (shippingCost, threshold) = await GetShippingSettingsAsync();
        return Ok(ToDto(cart, shippingCost, threshold, 0, null));
    }

    // مربوط به دکمه‌های + / - جلوی هر آیتم سبد خرید
    [HttpPut("items")]
    public async Task<ActionResult<CartDto>> UpdateItem([FromBody] UpdateCartItemRequestDto request)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.Id == request.CartItemId);
        if (item is null) return NotFound();

        if (request.Quantity <= 0)
        {
            cart.Items.Remove(item);
        }
        else
        {
            var availableStock = item.ProductVariantId.HasValue
                ? (item.ProductVariant?.StockQuantity ?? 0)
                : item.Product.StockQuantity;

            if (request.Quantity > availableStock)
            {
                return BadRequest(new
                {
                    message = availableStock <= 0
                        ? "این محصول موجود نیست"
                        : $"تنها {availableStock} عدد از این محصول موجود است"
                });
            }
            item.Quantity = request.Quantity;
        }

        await _db.SaveChangesAsync();

        var (shippingCost, threshold) = await GetShippingSettingsAsync();
        return Ok(ToDto(cart, shippingCost, threshold, 0, null));
    }

    [HttpDelete("items/{id:int}")]
    public async Task<ActionResult<CartDto>> RemoveItem(int id)
    {
        var cart = await GetOrCreateCartAsync();
        var item = cart.Items.FirstOrDefault(i => i.Id == id);
        if (item is not null)
        {
            cart.Items.Remove(item);
            await _db.SaveChangesAsync();
        }

        var (shippingCost, threshold) = await GetShippingSettingsAsync();
        return Ok(ToDto(cart, shippingCost, threshold, 0, null));
    }

    // مربوط به کادر کد تخفیف تو cart.html
    [HttpPost("apply-discount")]
    public async Task<ActionResult<CartDto>> ApplyDiscount([FromBody] ApplyDiscountRequestDto request)
    {
        var cart = await GetOrCreateCartAsync();
        var (shippingCost, threshold) = await GetShippingSettingsAsync();

        var discount = await _db.DiscountCodes.FirstOrDefaultAsync(d =>
            d.Code == request.Code && d.IsActive &&
            (d.ExpiresAt == null || d.ExpiresAt > DateTime.UtcNow));

        if (discount is null)
            return BadRequest(new { message = "کد تخفیف نامعتبر است" });

        if (discount.MaxUsageCount.HasValue && discount.UsedCount >= discount.MaxUsageCount)
            return BadRequest(new { message = "ظرفیت استفاده از این کد تمام شده است" });

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);

        if (discount.MinOrderAmount.HasValue && subtotal < discount.MinOrderAmount)
            return BadRequest(new { message = $"حداقل مبلغ سفارش برای این کد {discount.MinOrderAmount:N0} تومان است" });

        var discountAmount = discount.Type == DiscountType.Percent
            ? Math.Round(subtotal * discount.Amount / 100)
            : discount.Amount;

        return Ok(ToDto(cart, shippingCost, threshold, discountAmount, discount.Code));
    }

    private async Task<Cart> GetOrCreateCartAsync()
    {
        IQueryable<Cart> baseQuery = _db.Carts
            .Include(c => c.Items).ThenInclude(i => i.Product).ThenInclude(p => p.Images)
            .Include(c => c.Items).ThenInclude(i => i.ProductVariant);

        Cart? cart;

        if (_currentUser.UserId is int userId)
        {
            cart = await baseQuery.FirstOrDefaultAsync(c => c.UserId == userId);
            if (cart is null)
            {
                cart = new Cart { UserId = userId };
                _db.Carts.Add(cart);
                await _db.SaveChangesAsync();
            }
            return cart;
        }

        var guestSessionId = Request.Headers["X-Guest-Session-Id"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(guestSessionId))
            throw new InvalidOperationException("برای کاربر مهمان باید هدر X-Guest-Session-Id ارسال بشه");

        cart = await baseQuery.FirstOrDefaultAsync(c => c.GuestSessionId == guestSessionId);
        if (cart is null)
        {
            cart = new Cart { GuestSessionId = guestSessionId };
            _db.Carts.Add(cart);
            await _db.SaveChangesAsync();
        }
        return cart;
    }

    private async Task<(decimal shippingCost, decimal freeShippingThreshold)> GetShippingSettingsAsync()
    {
        var settings = await _db.AppSettings
            .Where(s => s.Key == "DefaultShippingCost" || s.Key == "FreeShippingThreshold")
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        var shippingCost = settings.TryGetValue("DefaultShippingCost", out var sc) ? decimal.Parse(sc) : 45000;
        var threshold = settings.TryGetValue("FreeShippingThreshold", out var th) ? decimal.Parse(th) : 5000000;

        return (shippingCost, threshold);
    }

    private static CartDto ToDto(Cart cart, decimal shippingCost, decimal freeShippingThreshold, decimal discountAmount, string? discountCode)
    {
        var items = cart.Items.Select(i => new CartItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductVariantId = i.ProductVariantId,
            ProductName = i.Product.Name,
            ImageUrl = i.Product.Images.FirstOrDefault(im => im.IsMain)?.ImageUrl
                       ?? i.Product.Images.FirstOrDefault()?.ImageUrl ?? "",
            VariantLabel = i.ProductVariant?.Color,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice
        }).ToList();

        var subtotal = items.Sum(i => i.TotalPrice);
        var actualShipping = subtotal == 0 ? 0 : (subtotal >= freeShippingThreshold ? 0 : shippingCost);

        return new CartDto
        {
            Id = cart.Id,
            Items = items,
            SubTotal = subtotal,
            ShippingCost = actualShipping,
            DiscountAmount = discountAmount,
            Total = Math.Max(subtotal + actualShipping - discountAmount, 0),
            AppliedDiscountCode = discountCode
        };
    }
}
