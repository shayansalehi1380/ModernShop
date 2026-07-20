namespace ModernShop.Core.Entities;

public class ProductVariant
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string? Color { get; set; }
    public string? Size { get; set; }
    public int StockQuantity { get; set; }
    public decimal? PriceAdjustment { get; set; }
}
