using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernShop.Core.DTOs
{
    public class ProductListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string CategoryName { get; set; } = null!;
        public string MainImageUrl { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
        public bool InStock { get; set; }
        public string? Badge { get; set; }   // "جدید" / "پرفروش" / "پیشنهاد ویژه" (محاسبه‌شده)
    }

    public class ProductImageDto
    {
        public string ImageUrl { get; set; } = null!;
        public bool IsMain { get; set; }
    }

    public class ProductVariantDto
    {
        public int Id { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public int StockQuantity { get; set; }
    }

    public class ProductSpecificationDto
    {
        public string Key { get; set; } = null!;
        public string Value { get; set; } = null!;
    }

    public class ProductReviewDto
    {
        public int Id { get; set; }
        public string UserFullName { get; set; } = null!;
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? Sku { get; set; }
        public string? Description { get; set; }
        public string CategoryName { get; set; } = null!;
        public string? BrandName { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }

        public List<ProductImageDto> Images { get; set; } = new();
        public List<ProductVariantDto> Variants { get; set; } = new();
        public List<ProductSpecificationDto> Specifications { get; set; } = new();
        public List<ProductReviewDto> Reviews { get; set; } = new();
    }

    public class CreateReviewRequestDto
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
    }
}
