using System.Text.Json;
using System.Text.Json.Serialization;

namespace ModernShop.Api.Services;

/// <summary>
/// همه‌ی تاریخ‌های این پروژه با DateTime.UtcNow ذخیره می‌شن، ولی SQL Server مقدار Kind رو نگه نمی‌داره
/// و وقتی EF Core دوباره می‌خونتش، Kind میشه Unspecified. اگه همون‌جوری سریالایز بشه (بدون "Z")،
/// جاوااسکریپت (new Date(...)) اشتباهی فکر می‌کنه این تاریخ از قبل به‌وقت محلی مرورگره، نه UTC؛
/// و همین باعث میشه ساعت‌های نمایش داده‌شده تو فرانت (مثلا جزئیات سفارش تو پنل مدیریت) چند ساعت
/// جلو/عقب باشن. این کانورتر تضمین می‌کنه هر DateTime همیشه با پسوند "Z" (UTC) سریالایز بشه، تا
/// مرورگر خودش درست تبدیلش کنه به ساعت محلی کاربر.
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : DateTime.SpecifyKind(reader.GetDateTime(), DateTimeKind.Utc);

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }
}
