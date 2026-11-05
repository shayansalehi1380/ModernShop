using System.Text.Json.Serialization;

namespace ModernShop.Api.Services;

public class SnappshopEnvelope<T>
{
    [JsonPropertyName("status")] public bool Status { get; set; }
    [JsonPropertyName("data")] public T? Data { get; set; }
    [JsonPropertyName("meta")] public SnappshopMeta? Meta { get; set; }
    [JsonPropertyName("message")] public string? Message { get; set; }
    [JsonPropertyName("code")] public int? Code { get; set; }
}

public class SnappshopMeta
{
    [JsonPropertyName("pagination")] public SnappshopPagination? Pagination { get; set; }
}

public class SnappshopPagination
{
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("current_page")] public int CurrentPage { get; set; }
    [JsonPropertyName("total_pages")] public int TotalPages { get; set; }
}

public class SnappshopVendor
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("title_en")] public string? TitleEn { get; set; }
    [JsonPropertyName("status")] public string? Status { get; set; }
}

public class SnappshopDiscount
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("special_price")] public long SpecialPrice { get; set; }
    [JsonPropertyName("stock")] public int Stock { get; set; }
    [JsonPropertyName("percent")] public int Percent { get; set; }
    [JsonPropertyName("start_at")] public string? StartAt { get; set; }
    [JsonPropertyName("end_at")] public string? EndAt { get; set; }
}

public class SnappshopProduct
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("sku")] public string? Sku { get; set; }
    [JsonPropertyName("active")] public bool Active { get; set; }
    [JsonPropertyName("stock")] public int Stock { get; set; }
    [JsonPropertyName("warehouse_stock")] public int? WarehouseStock { get; set; }
    [JsonPropertyName("capacity")] public int? Capacity { get; set; }
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("price")] public long Price { get; set; }
    [JsonPropertyName("discount")] public SnappshopDiscount? Discount { get; set; }
}

/// آیتم آپدیت. اگر Sku پر باشد به جای Id استفاده می‌شود (طبق مستندات، sku اولویت دارد).
public class SnappshopUpdateItem
{
    [JsonPropertyName("id"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Id { get; set; }

    [JsonPropertyName("sku"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Sku { get; set; }

    [JsonPropertyName("stock")] public int Stock { get; set; }          // اجباری
    [JsonPropertyName("price")] public long Price { get; set; }         // اجباری، به تومان

    [JsonPropertyName("capacity"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Capacity { get; set; }

    [JsonPropertyName("special_price"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? SpecialPrice { get; set; }

    [JsonPropertyName("special_price_start_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SpecialPriceStartAt { get; set; }   // میلادی: 2026-11-05

    [JsonPropertyName("special_price_end_at"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SpecialPriceEndAt { get; set; }

    [JsonPropertyName("special_price_stock"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? SpecialPriceStock { get; set; }
}

public class SnappshopUpdateResult
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("sku")] public string? Sku { get; set; }
    [JsonPropertyName("status")] public bool Status { get; set; }
    [JsonPropertyName("messages")] public List<string> Messages { get; set; } = new();
}

public class SnappshopApiException : Exception
{
    public int HttpStatus { get; }
    public int? ApiCode { get; }
    public SnappshopApiException(string message, int httpStatus, int? apiCode = null)
        : base(message) { HttpStatus = httpStatus; ApiCode = apiCode; }
}