namespace ModernShop.Api.Services;

/// <summary>
/// مسیر فیزیکی واقعی که آپلودها (بنر/برند/دسته‌بندی/محصول) توش ذخیره می‌شن؛ از appsettings.json
/// (کلید UploadsPath) یا در نبود اون، wwwroot/uploads پیش‌فرض قدیمی.
/// </summary>
public class UploadsSettings
{
    public UploadsSettings(string root)
    {
        Root = root;
    }

    public string Root { get; }
}
