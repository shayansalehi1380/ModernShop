using ModernShop.Core.DTOs;
using ModernShop.Core.Entities;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Atelier.Api.Controllers;

[ApiController]
[Route("api/account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public AccountController(AppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // مربوط به تب «اطلاعات حساب کاربری» تو account.html
    [HttpGet("profile")]
    public async Task<ActionResult<UserProfileDto>> GetProfile()
    {
        var user = await _db.Users.FindAsync(_currentUser.UserId!.Value);
        if (user is null) return NotFound();

        return Ok(new UserProfileDto
        {
            Id = user.Id,
            PhoneNumber = user.PhoneNumber,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            BirthDate = user.BirthDate,
            Gender = user.Gender,
            LoyaltyTier = user.LoyaltyTier
        });
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        var user = await _db.Users.FindAsync(_currentUser.UserId!.Value);
        if (user is null) return NotFound();

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.Email = request.Email;
        user.BirthDate = request.BirthDate;
        user.Gender = request.Gender;

        await _db.SaveChangesAsync();
        return Ok(new { message = "اطلاعات با موفقیت ذخیره شد" });
    }

    [HttpGet("addresses")]
    public async Task<ActionResult<List<AddressDto>>> GetAddresses()
    {
        var userId = _currentUser.UserId!.Value;

        var addresses = await _db.Addresses
            .Where(a => a.UserId == userId)
            .Select(a => new AddressDto
            {
                Id = a.Id,
                ReceiverFullName = a.ReceiverFullName,
                ReceiverPhone = a.ReceiverPhone,
                Province = a.Province,
                City = a.City,
                FullAddress = a.FullAddress,
                PostalCode = a.PostalCode,
                IsDefault = a.IsDefault
            })
            .ToListAsync();

        return Ok(addresses);
    }

    // مربوط به فرم آدرس تو checkout.html (هم افزودن، هم ویرایش - بسته به وجود Id)
    [HttpPost("addresses")]
    public async Task<ActionResult<AddressDto>> UpsertAddress([FromBody] UpsertAddressRequestDto request)
    {
        var userId = _currentUser.UserId!.Value;

        Address address;
        if (request.Id.HasValue)
        {
            var existing = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == request.Id && a.UserId == userId);
            if (existing is null) return NotFound();
            address = existing;
        }
        else
        {
            address = new Address { UserId = userId };
            _db.Addresses.Add(address);
        }

        address.ReceiverFullName = request.ReceiverFullName;
        address.ReceiverPhone = request.ReceiverPhone;
        address.Province = request.Province;
        address.City = request.City;
        address.FullAddress = request.FullAddress;
        address.PostalCode = request.PostalCode;

        if (request.IsDefault)
        {
            var others = await _db.Addresses.Where(a => a.UserId == userId && a.Id != address.Id).ToListAsync();
            foreach (var other in others) other.IsDefault = false;
            address.IsDefault = true;
        }

        await _db.SaveChangesAsync();

        return Ok(new AddressDto
        {
            Id = address.Id,
            ReceiverFullName = address.ReceiverFullName,
            ReceiverPhone = address.ReceiverPhone,
            Province = address.Province,
            City = address.City,
            FullAddress = address.FullAddress,
            PostalCode = address.PostalCode,
            IsDefault = address.IsDefault
        });
    }

    [HttpDelete("addresses/{id:int}")]
    public async Task<IActionResult> DeleteAddress(int id)
    {
        var userId = _currentUser.UserId!.Value;
        var address = await _db.Addresses.FirstOrDefaultAsync(a => a.Id == id && a.UserId == userId);
        if (address is null) return NotFound();

        _db.Addresses.Remove(address);
        await _db.SaveChangesAsync();
        return Ok(new { message = "آدرس حذف شد" });
    }

    // مربوط به تب «علاقه‌مندی‌ها» تو account.html
    [HttpGet("wishlist")]
    public async Task<ActionResult<List<WishlistItemDto>>> GetWishlist()
    {
        var userId = _currentUser.UserId!.Value;

        var items = await _db.WishlistItems
            .Where(w => w.UserId == userId)
            .Select(w => new WishlistItemDto
            {
                ProductId = w.ProductId,
                ProductName = w.Product.Name,
                ImageUrl = w.Product.Images.Where(i => i.IsMain).Select(i => i.ImageUrl).FirstOrDefault()
                           ?? w.Product.Images.Select(i => i.ImageUrl).FirstOrDefault() ?? "",
                Price = w.Product.DiscountPrice ?? w.Product.Price
            })
            .ToListAsync();

        return Ok(items);
    }

    // مربوط به دکمه قلب روی کارت محصول
    [HttpPost("wishlist/{productId:int}")]
    public async Task<IActionResult> AddToWishlist(int productId)
    {
        var userId = _currentUser.UserId!.Value;

        var exists = await _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.ProductId == productId);
        if (exists) return Ok(new { message = "قبلاً به علاقه‌مندی‌ها اضافه شده" });

        var productExists = await _db.Products.AnyAsync(p => p.Id == productId);
        if (!productExists) return NotFound();

        _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
        await _db.SaveChangesAsync();

        return Ok(new { message = "به علاقه‌مندی‌ها اضافه شد" });
    }

    [HttpDelete("wishlist/{productId:int}")]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        var userId = _currentUser.UserId!.Value;
        var item = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (item is null) return NotFound();

        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync();
        return Ok(new { message = "از علاقه‌مندی‌ها حذف شد" });
    }
}
