using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModernShop.Core.Enums;

namespace ModernShop.Core.DTOs
{
    public class CreateOrderRequestDto
    {
        public int AddressId { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? DiscountCode { get; set; }
    }

    public class OrderItemDto
    {
        public string ProductName { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderStatusHistoryDto
    {
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }

        public List<OrderItemDto> Items { get; set; } = new();
        public List<OrderStatusHistoryDto> StatusHistory { get; set; } = new();
    }

    public class OrderListItemDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public OrderStatus Status { get; set; }
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public string ThumbnailImageUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
