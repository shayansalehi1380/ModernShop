using ModernShop.Core.Enums;

namespace ModernShop.Core.Entities;

public class User
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = null!;   // یکتا، ۱۱ رقمی
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public DateTime? BirthDate { get; set; }
    public Gender Gender { get; set; } = Gender.NotSpecified;
    public bool IsPhoneVerified { get; set; }
    public bool IsActive { get; set; } = true;

    public string? Bio { get; set; }                                   // برای کاربرانی که نویسنده وبلاگ هم هستن
    public LoyaltyTier LoyaltyTier { get; set; } = LoyaltyTier.Bronze;  // نشان "مشتری طلایی" و مشابه

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Address> Addresses { get; set; } = new List<Address>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
    public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
    public ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
    public ICollection<BlogComment> BlogComments { get; set; } = new List<BlogComment>();
}
