using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ModernShop.Infrastructure.Data;

/// <summary>
/// این کلاس فقط موقع اجرای دستورات Migration (dotnet ef migrations add ...) استفاده می‌شه.
/// وجودش باعث می‌شه بتونی همین الان، قبل از ساخته‌شدن Atelier.Api، مایگریشن اولیه رو بسازی
/// و بعداً که Api رو اضافه کردیم، فقط کانکشن‌استرینگ واقعی از appsettings.json خونده می‌شه.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // این مقدار فقط برای زمان ساخت Migration استفاده می‌شه؛ کانکشن واقعی برنامه
        // بعداً از appsettings.json پروژه Atelier.Api خونده می‌شه (طبق DependencyInjection.cs)
        const string designTimeConnectionString =
            "Server=localhost;Database=AtelierDb;Trusted_Connection=True;TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(designTimeConnectionString);
        
        return new AppDbContext(optionsBuilder.Options);
    }
}
