using System.Text.Json.Serialization;

namespace ModernShop.Core.DTOs;

// دقیقاً مطابق مستندات TorobAPI v3 - اسم فیلدها باید عیناً همینا باشه (snake_case)،
// برای همین [JsonPropertyName] رو صریح گذاشتیم تا به‌جای camelCase پیش‌فرض پروژه ازش استفاده بشه
public class TorobRequestDto
{
    [JsonPropertyName("page")] public int? Page { get; set; }
    [JsonPropertyName("sort")] public string? Sort { get; set; }
    [JsonPropertyName("page_urls")] public List<string>? PageUrls { get; set; }
    [JsonPropertyName("page_uniques")] public List<string>? PageUniques { get; set; }
}

public class TorobProductDto
{
    [JsonPropertyName("page_unique")] public string PageUnique { get; set; } = null!;
    [JsonPropertyName("page_url")] public string PageUrl { get; set; } = null!;
    [JsonPropertyName("product_group_id")] public string? ProductGroupId { get; set; }
    [JsonPropertyName("title")] public string Title { get; set; } = null!;
    [JsonPropertyName("subtitle")] public string? Subtitle { get; set; }
    [JsonPropertyName("current_price")] public long CurrentPrice { get; set; }
    [JsonPropertyName("old_price")] public long? OldPrice { get; set; }
    [JsonPropertyName("availability")] public bool Availability { get; set; }
    [JsonPropertyName("category_name")] public string? CategoryName { get; set; }
    [JsonPropertyName("image_links")] public List<string> ImageLinks { get; set; } = new();
    [JsonPropertyName("short_desc")] public string? ShortDesc { get; set; }
    [JsonPropertyName("spec")] public Dictionary<string, string> Spec { get; set; } = new();
    [JsonPropertyName("guarantee")] public string? Guarantee { get; set; }
    [JsonPropertyName("date_added")] public string DateAdded { get; set; } = null!;
    [JsonPropertyName("date_updated")] public string DateUpdated { get; set; } = null!;
}

public class TorobResultDto
{
    [JsonPropertyName("api_version")] public string ApiVersion { get; set; } = "torob_api_v3";
    [JsonPropertyName("current_page")] public int CurrentPage { get; set; }
    [JsonPropertyName("total")] public int Total { get; set; }
    [JsonPropertyName("max_pages")] public int MaxPages { get; set; }
    [JsonPropertyName("products")] public List<TorobProductDto> Products { get; set; } = new();
}

public class TorobErrorDto
{
    [JsonPropertyName("error")] public string Error { get; set; } = null!;
}
