using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernShop.Core.DTOs
{
    public class CartItemDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public string? VariantLabel { get; set; }   // مثلا "رنگ مشکی"
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => UnitPrice * Quantity;
    }

    public class CartDto
    {
        public int Id { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal Total { get; set; }
        public string? AppliedDiscountCode { get; set; }
    }

    public class AddToCartRequestDto
    {
        public int ProductId { get; set; }
        public int? ProductVariantId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemRequestDto
    {
        public int CartItemId { get; set; }
        public int Quantity { get; set; }
    }

    public class ApplyDiscountRequestDto
    {
        public string Code { get; set; } = null!;
    }
}
