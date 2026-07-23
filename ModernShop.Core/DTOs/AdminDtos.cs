using ModernShop.Core.Enums;

namespace ModernShop.Core.DTOs
{
    public class AdminLoginRequestDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    // ---------------- محصولات ----------------

    public class AdminProductListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Sku { get; set; }
        public string CategoryName { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public bool IsVariable { get; set; }
    }

    public class AdminProductSpecDto
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class AdminProductVariantDto
    {
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int StockQuantity { get; set; }
        public decimal? PriceAdjustment { get; set; }
    }

    public class AdminProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public string? ImageUrl { get; set; }
        public List<AdminProductSpecDto> Specifications { get; set; } = new();
        public List<AdminProductVariantDto> Variants { get; set; } = new();
    }

    public class SaveProductRequestDto
    {
        public string Name { get; set; } = null!;
        public string? Slug { get; set; }
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public int CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ImageUrl { get; set; }
        public List<AdminProductSpecDto> Specifications { get; set; } = new();
        public List<AdminProductVariantDto> Variants { get; set; } = new();
    }

    // ---------------- وبلاگ ----------------

    public class AdminBlogPostListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public int ReadTimeMinutes { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public int ViewCount { get; set; }
    }

    public class AdminBlogPostDetailDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Excerpt { get; set; }
        public string Content { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public int BlogCategoryId { get; set; }
        public int ReadTimeMinutes { get; set; }
        public bool IsPublished { get; set; }
    }

    public class SaveBlogPostRequestDto
    {
        public string Title { get; set; } = null!;
        public string? Slug { get; set; }
        public string? Excerpt { get; set; }
        public string Content { get; set; } = null!;
        public string? FeaturedImageUrl { get; set; }
        public int BlogCategoryId { get; set; }
        public int ReadTimeMinutes { get; set; } = 1;
        public bool IsPublished { get; set; }
    }

    // ---------------- سفارشات ----------------

    public class AdminOrderListItemDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;
        public int ItemCount { get; set; }
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminOrderItemDto
    {
        public string ProductName { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class AdminOrderStatusHistoryDto
    {
        public OrderStatus Status { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AdminOrderDetailDto
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public OrderStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }
        public string? DiscountCode { get; set; }

        public string CustomerName { get; set; } = null!;
        public string CustomerPhone { get; set; } = null!;
        public string ShippingFullName { get; set; } = null!;
        public string ShippingPhone { get; set; } = null!;
        public string ShippingCity { get; set; } = null!;
        public string ShippingFullAddress { get; set; } = null!;
        public string ShippingPostalCode { get; set; } = null!;

        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public List<AdminOrderItemDto> Items { get; set; } = new();
        public List<AdminOrderStatusHistoryDto> StatusHistory { get; set; } = new();
    }

    public class UpdateOrderStatusRequestDto
    {
        public OrderStatus Status { get; set; }
    }
}
