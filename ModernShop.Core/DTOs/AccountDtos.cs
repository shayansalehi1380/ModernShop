using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ModernShop.Core.Enums;

namespace ModernShop.Core.DTOs
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? BirthDate { get; set; }
        public Gender Gender { get; set; }
        public LoyaltyTier LoyaltyTier { get; set; }
    }

    public class UpdateProfileRequestDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public DateTime? BirthDate { get; set; }
        public Gender Gender { get; set; }
    }

    public class AddressDto
    {
        public int Id { get; set; }
        public string ReceiverFullName { get; set; } = null!;
        public string ReceiverPhone { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string City { get; set; } = null!;
        public string FullAddress { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public bool IsDefault { get; set; }
    }

    public class UpsertAddressRequestDto
    {
        public int? Id { get; set; }   // null یعنی آدرس جدید
        public string ReceiverFullName { get; set; } = null!;
        public string ReceiverPhone { get; set; } = null!;
        public string Province { get; set; } = null!;
        public string City { get; set; } = null!;
        public string FullAddress { get; set; } = null!;
        public string PostalCode { get; set; } = null!;
        public bool IsDefault { get; set; }
    }

    public class WishlistItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public decimal Price { get; set; }
    }

}
