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

    // فقط تعداد آیتم‌های سبد (نشان روی آیکون سبد خرید تو هدر/منوی موبایل که تقریباً تو هر
    // صفحه‌ای صدا زده می‌شه)؛ برخلاف GetCart بالا، هیچ رابطه‌ای (محصول/عکس/تنوع) لود نمی‌کنه
    [HttpGet("count")]
    public async Task<ActionResult<object>> GetCartItemCount()
    {
        int count;
        if (_currentUser.UserId is int userId)
        {
            count = await _db.CartItems.AsNoTracking()
                .Where(ci => ci.Cart.UserId == userId)
                .SumAsync(ci => (int?)ci.Quantity) ?? 0;
        }
        else
        {
            var guestSessionId = Request.Headers["X-Guest-Session-Id"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(guestSessionId)) return Ok(new { count = 0 });

            count = await _db.CartItems.AsNoTracking()
                .Where(ci => ci.Cart.GuestSessionId == guestSessionId)
                .SumAsync(ci => (int?)ci.Quantity) ?? 0;
        }
        return Ok(new { count });
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
        var totalStock = variant?.StockQuantity ?? product.StockQuantity;

        var cart = await GetOrCreateCartAsync();

        var existing = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId && i.ProductVariantId == request.ProductVariantId);

        // موجودی واقعاً در دسترس = کل موجودی منهای چیزی که همین الان تو سبد بقیه‌ی کاربرها رزرو شده
        // (سبد خود همین کاربر از این حساب مستثناست، چون داره همون رزرو خودش رو زیاد می‌کنه)
        var availableStock = await GetAvailableStockAsync(request.ProductId, request.ProductVariantId, totalStock, cart.Id);

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

        var reservedUntil = DateTime.UtcNow.AddHours(2);

        if (existing is not null)
        {
            existing.Quantity = requestedTotal;
            existing.ReservedUntil = reservedUntil;
        }
        else
        {
            cart.Items.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = request.ProductId,
                ProductVariantId = request.ProductVariantId,
                Quantity = request.Quantity,
                UnitPrice = (variant?.PriceAdjustment ?? 0) + (product.DiscountPrice ?? product.Price),
                ReservedUntil = reservedUntil
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
            var totalStock = item.ProductVariantId.HasValue
                ? (item.ProductVariant?.StockQuantity ?? 0)
                : item.Product.StockQuantity;

            // خود همین ردیف (رزرو فعلی همین کاربر برای همین محصول/تنوع) از حساب «رزروشده توسط بقیه» مستثناست
            var availableStock = await GetAvailableStockAsync(item.ProductId, item.ProductVariantId, totalStock, cart.Id);

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
            item.ReservedUntil = DateTime.UtcNow.AddHours(2);
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

    // موجودی واقعاً در دسترس یک محصول/تنوع = کل موجودی منهای مجموع تعداد رزروشده تو سبد بقیه‌ی
    // کاربرها (سبدهایی غیر از excludeCartId) که هنوز منقضی نشدن (ReservedUntil > الان). این
    // همون مکانیزمی هست که جلوی فروش بیش از موجودی واقعی رو موقع افزودن/تغییر تعداد سبد می‌گیره؛
    // رزروهای منقضی‌شده (کسی که ۲ ساعت پیش اضافه کرده و خرید نکرده) خودکار از این حساب کنار میرن.
    private async Task<int> GetAvailableStockAsync(int productId, int? productVariantId, int totalStock, int excludeCartId)
    {
        var now = DateTime.UtcNow;
        var reservedByOthers = await _db.CartItems.AsNoTracking()
            .Where(ci => ci.ProductId == productId
                      && ci.ProductVariantId == productVariantId
                      && ci.CartId != excludeCartId
                      && ci.ReservedUntil > now)
            .SumAsync(ci => (int?)ci.Quantity) ?? 0;

        return totalStock - reservedByOthers;
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
            ProductSlug = i.Product.Slug,
            ImageUrl = i.Product.Images.FirstOrDefault(im => im.IsMain)?.ImageUrl
                       ?? i.Product.Images.FirstOrDefault()?.ImageUrl ?? "",
            VariantLabel = i.ProductVariant is null
                ? null
                : (!string.IsNullOrEmpty(i.ProductVariant.Size) && i.ProductVariant.Size != "-"
                    ? $"{i.ProductVariant.Color} · سایز {i.ProductVariant.Size}"
                    : i.ProductVariant.Color),
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
