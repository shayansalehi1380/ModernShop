namespace ModernShop.Api.Services;

public class SnappshopSettings
{
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://apix.snappshop.ir/automation/v1";
    public string AgentUser { get; set; } = "";
    public string Token { get; set; } = "";
    public string VendorId { get; set; } = "";
    public int SyncIntervalMinutes { get; set; } = 15;
    public int BatchSize { get; set; } = 50; // سقف مجاز اسنپ‌شاپ: ۵۰
}