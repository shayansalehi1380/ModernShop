using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModernShop.Core.DTOs
{
    public class ProductFilterRequestDto
    {
        public List<int>? CategoryIds { get; set; }
        public List<int>? BrandIds { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public double? MinRating { get; set; }
        public bool InStockOnly { get; set; }
        public string SortBy { get; set; } = "newest";  // newest | bestselling | cheap | expensive | rating
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;         // مطابق منطق فروشگاه: هر صفحه حداکثر ۵۰ محصول
    }

    public class PagedResultDto<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    }
}
