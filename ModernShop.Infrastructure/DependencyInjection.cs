using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure.Data;
using ModernShop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ModernShop.Infrastructure;

/// <summary>
/// این extension method قراره از Program.cs پروژه Atelier.Api صدا زده بشه:
///   builder.Services.AddInfrastructure(builder.Configuration);
/// همه‌ی وابستگی‌های این لایه (دیتابیس، پیامک، درگاه پرداخت) یک‌جا اینجا ثبت می‌شن.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.Configure<SmsSettings>(configuration.GetSection("Sms"));
        services.Configure<ZarinpalSettings>(configuration.GetSection("Zarinpal"));

        services.AddHttpClient<ISmsService, SmsService>();
        services.AddHttpClient<IPaymentGatewayService, ZarinpalPaymentService>();

        return services;
    }
}
