using System.Text;
using System.Threading.RateLimiting;
using ModernShop.Api.Services;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// ===== لایه Infrastructure: DbContext + سرویس پیامک + سرویس درگاه پرداخت =====
builder.Services.AddInfrastructure(builder.Configuration);

// ===== سرویس‌های مخصوص همین لایه Api =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("Admin"));
builder.Services.AddScoped<SeoPageRenderer>();
builder.Services.AddHostedService<CartReservationCleanupService>();

// آپلودها (بنر/برند/دسته‌بندی/محصول) رو از یک مسیر فیزیکی قابل‌تنظیم سرو می‌کنیم، نه لزوماً
// wwwroot/uploads خود پروژه؛ چون هر بار publish/ری‌دیپلوی معمولاً کل پوشه‌ی سایت رو با نسخه‌ی
// جدید جایگزین می‌کنه و اگه آپلودها همون داخل پوشه‌ی سایت باشن، هر عکسی که از پنل مدیریت رو
// خود سرور آپلود شده (نه از قبل تو سورس بوده) با ری‌دیپلوی بعدی از بین می‌ره. با تنظیم
// UploadsPath تو appsettings.json به یک مسیر مطلق و خارج از پوشه‌ی سایت، این مشکل حل می‌شه.
// اگه تنظیم نشه (پیش‌فرض)، دقیقاً رفتار قبلی (wwwroot/uploads) حفظ می‌شه.
var configuredUploadsPath = builder.Configuration["UploadsPath"];
var uploadsRoot = string.IsNullOrWhiteSpace(configuredUploadsPath)
    ? Path.Combine(builder.Environment.WebRootPath, "uploads")
    : (Path.IsPathRooted(configuredUploadsPath) ? configuredUploadsPath : Path.Combine(builder.Environment.ContentRootPath, configuredUploadsPath));
Directory.CreateDirectory(uploadsRoot);
builder.Services.AddSingleton(new UploadsSettings(uploadsRoot));

// ===== احراز هویت با JWT =====
var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("بخش Jwt در appsettings.json تنظیم نشده است");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization(options =>
{
    // فقط توکن‌هایی که JwtTokenService.GenerateAdminToken تولید کرده (پنل مدیریت) اجازه دارن
    options.AddPolicy("AdminOnly", policy => policy.RequireClaim("scope", "admin"));
});

// ===== محدودیت نرخ درخواست: جلوگیری از brute-force روی ورود ادمین و کد تایید پیامکی =====
// هر پالیسی بر اساس IP کلاینت پارتیشن می‌شه؛ بعد از پر شدن سهمیه، درخواست فوراً با ۴۲۹ رد
// می‌شه (QueueLimit=0)، نه اینکه تو صف بمونه.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("AdminLogin", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));

    // ارسال کد: جلوگیری از سوءاستفاده/هزینه‌تراشی روی پنل پیامکی (اسپم ارسال کد به یک یا چند شماره)
    options.AddPolicy("OtpSend", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(10),
            QueueLimit = 0
        }));

    // تایید کد: کد ۵ رقمیه (۱۰۰٬۰۰۰ حالت)؛ بدون این محدودیت با اسکریپت قابل حدس زدنه
    options.AddPolicy("OtpVerify", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 8,
            Window = TimeSpan.FromMinutes(5),
            QueueLimit = 0
        }));
});

// ===== HSTS قوی‌تر از پیش‌فرض (۳۰ روز → ۱ سال + زیردامنه‌ها) =====
// نکته: Preload=true فقط مقدار هدر رو درست می‌کنه، به‌تنهایی دامنه رو تو لیست پیش‌بارگذاری
// مرورگرها ثبت نمی‌کنه؛ برای ثبت واقعی باید دستی از طریق hstspreload.org اقدام بشه (اختیاریه
// و بعد از اطمینان کامل از درست کار کردن HTTPS رو کل دامنه/زیردامنه‌ها توصیه می‌شه)
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

// ===== CORS: چون فرانت (فایل‌های HTML) روی دامنه/پورت جدا اجرا می‌شه =====
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// ===== Controllers + Swagger =====
builder.Services.AddControllers().AddJsonOptions(options =>
{
    // همه‌ی تاریخ‌ها UtcNow ذخیره می‌شن؛ این کانورترها تضمین می‌کنن همیشه با "Z" سریالایز بشن
    // تا فرانت (new Date(...)) درست به‌وقت محلی کاربر تبدیلشون کنه (نه چند ساعت جلو/عقب).
    options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
    options.JsonSerializerOptions.Converters.Add(new NullableUtcDateTimeConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Atelier API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "توکن JWT رو به فرمت Bearer {token} وارد کن"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(); // در آدرس /swagger قابل مشاهده‌ست
}
else
{
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { message = "خطای داخلی سرور رخ داد" });
        });
    });

    // به مرورگر می‌گه برای یک سال همیشه از HTTPS استفاده کنه، حتی اگه کاربر آدرس رو با http:// باز کنه
    app.UseHsts();
}

// هدرهای امنیتی پایه‌ای که به‌صورت پیش‌فرض تو ASP.NET Core فرستاده نمی‌شن؛ روی همه‌ی جواب‌ها
// (فایل‌های استاتیک + API) اعمال می‌شن
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
    // از اینکه یه تب دیگه (که از همین صفحه با window.open باز شده) بتونه به window.opener همین
    // صفحه دسترسی پیدا کنه جلوگیری می‌کنه
    context.Response.Headers.Append("Cross-Origin-Opener-Policy", "same-origin");
    // نکته‌ی مهم: چون کل سایت (نه فقط این پروژه، بلکه معماری فعلیش) از اسکریپت/استایل inline
    // تو خود صفحات HTML استفاده می‌کنه (نه فایل‌های جدا)، این CSP باید 'unsafe-inline' رو برای
    // script-src/style-src مجاز کنه وگرنه کل سایت از کار می‌افته. یعنی این نسخه بیشتر جلوی
    // تزریق منابع از دامنه‌های ناشناس (script/style/iframe از یه سایت دیگه) رو می‌گیره، نه
    // جلوی XSS مبتنی بر اسکریپت inline - برای اون سطح از محافظت، باید در آینده همه‌ی
    // <script> های تو خود صفحات به فایل‌های خارجی منتقل بشن (یه پروژه‌ی جداگانه و بزرگ‌تر)
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://cdn.ckeditor.com; " +
        "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com; " +
        "img-src 'self' data: https:; " +
        "connect-src 'self'; " +
        "frame-ancestors 'none'; " +
        "base-uri 'self'; " +
        "form-action 'self'");
    await next();
});

app.UseHttpsRedirection();

// صفحه اصلی سایت به "/home" منتقل شده (قبلاً index.html)؛ ریشه‌ی سایت واقعاً (نه فقط داخلی)
// به /home ریدایرکت می‌شه تا در نوار آدرس مرورگر هم "home" دیده بشه
app.Use(async (context, next) =>
{
    if (context.Request.Path == "/")
    {
        context.Response.Redirect("/home", permanent: false);
        return;
    }
    await next();
});

// سئوی صفحه محصول/مطلب وبلاگ: ربات‌های AI (GPTBot، ClaudeBot و ...) برخلاف گوگل جاوااسکریپت
// اجرا نمی‌کنن، پس بدون این میان‌افزار فقط یه اسکلت خالی می‌دیدن. اینجا قبل از سرو شدن فایل
// استاتیک، عنوان/توضیح‌متا/Schema.org رو با اطلاعات واقعی محصول یا مطلب از دیتابیس پر می‌کنیم -
// ظاهر صفحه برای کاربر واقعی هیچ فرقی نمی‌کنه (همون HTML همیشگی + جاوااسکریپت همیشگی)
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    var slug = context.Request.Query["slug"].FirstOrDefault();

    if (!string.IsNullOrWhiteSpace(slug) && (path == "/product" || path == "/blog-post"))
    {
        var renderer = context.RequestServices.GetRequiredService<SeoPageRenderer>();
        var handled = path == "/product"
            ? await renderer.TryRenderProductAsync(context, slug)
            : await renderer.TryRenderBlogPostAsync(context, slug);
        if (handled) return;
    }

    await next();
});

// حذف پسوند .html از آدرس صفحات: وقتی مسیر پسوند نداره (مثلاً /shop یا /product) و فایل
// متناظرش با .html تو wwwroot وجود داره، فقط مسیر داخلی درخواست عوض می‌شه (نه ریدایرکت)
// تا هم نوار آدرس مرورگر همیشه بدون .html بمونه، هم لینک‌های api/swagger/فایل‌های استاتیک دیگه دست‌نخورده بمونن
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (!string.IsNullOrEmpty(path)
        && !path.StartsWith("/api", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase)
        && !System.IO.Path.HasExtension(path))
    {
        var htmlPath = path.TrimEnd('/') + ".html";
        if (app.Environment.WebRootFileProvider.GetFileInfo(htmlPath).Exists)
        {
            context.Request.Path = htmlPath;
        }
    }
    await next();
});

// تامبنیل تصاویر محصول: /uploads/products/thumb/{width}/{filename} اولین‌بار که درخواست
// بشه، از فایل اصلی (که ممکنه چندمگابایتی باشه) یه نسخه‌ی کوچیک‌شده می‌سازه و رو دیسک کش
// می‌کنه؛ درخواست‌های بعدیِ همون آدرس مستقیم توسط UseStaticFiles زیر همین میان‌افزار سرو
// می‌شن (بدون هیچ پردازش عکسی) - یعنی به‌سرعت هر فایل استاتیک دیگه. این باعث می‌شه کارت
// محصول همه‌جای سایت به‌جای عکس اصلی، نسخه‌ی سبک‌شده رو بارگذاری کنه، حتی برای عکس‌هایی که
// از قبل (قبل از این تغییر) آپلود شدن.
var allowedThumbWidths = new HashSet<int> { 96, 160, 240, 320, 480, 640, 960, 1200 };
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    if (path is not null && path.StartsWith("/uploads/products/thumb/", StringComparison.OrdinalIgnoreCase))
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 5 && int.TryParse(segments[3], out var width) && allowedThumbWidths.Contains(width))
        {
            var fileName = segments[4];
            var ext = Path.GetExtension(fileName);
            // gif رو دست‌نخورده نگه می‌داریم که انیمیشنش خراب نشه؛ بقیه‌ی فرمت‌ها (PNG/JPEG و...)
            // به WebP تبدیل می‌شن - همون فرمتی که گزارش Lighthouse به‌جای PNG/JPEG بزرگ خواسته بود
            var isGif = string.Equals(ext, ".gif", StringComparison.OrdinalIgnoreCase);
            var thumbFileName = isGif ? fileName : Path.GetFileNameWithoutExtension(fileName) + ".webp";

            var originalPath = Path.Combine(uploadsRoot, "products", fileName);
            var thumbDir = Path.Combine(uploadsRoot, "products", "thumb", width.ToString());
            var thumbPath = Path.Combine(thumbDir, thumbFileName);

            if (!File.Exists(thumbPath) && File.Exists(originalPath))
            {
                Directory.CreateDirectory(thumbDir);
                try
                {
                    using var image = await Image.LoadAsync(originalPath);
                    image.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(width, width) }));
                    if (isGif)
                        await image.SaveAsync(thumbPath);
                    else
                        await image.SaveAsWebpAsync(thumbPath);
                }
                catch (Exception ex)
                {
                    // اگه پردازش عکس شکست خورد، thumbPath رو نمی‌سازیم (وگرنه این شکست برای همیشه کش می‌شد
                    // و دیگه هیچ‌وقت دوباره امتحان نمی‌شد)؛ فقط همین یک درخواست از رو فایل اصلی سرو می‌شه
                    // و خطا لاگ می‌شه تا قابل بررسی باشه
                    var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("ThumbnailMiddleware");
                    logger.LogWarning(ex, "Thumbnail generation failed for {FileName} at width {Width}; serving original file for this request", fileName, width);
                    context.Request.Path = $"/uploads/products/{fileName}";
                }
            }

            // چه همین الان ساخته شده باشه چه از قبل تو کش بوده، چون اسم فایل تولیدشده ممکنه پسوندش
            // با اسم اصلی درخواست فرق کنه (webp به‌جای png/jpg)، مسیر درخواست رو به اسم واقعی فایل
            // کش‌شده اصلاح می‌کنیم تا UseStaticFiles با Content-Type درست (image/webp) سروش کنه
            if (File.Exists(thumbPath))
            {
                context.Request.Path = $"/uploads/products/thumb/{width}/{thumbFileName}";
            }
        }
    }
    await next();
});

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "home.html" }
});

// /uploads از uploadsRoot سرو می‌شه (که ممکنه بیرون از wwwroot باشه، توضیح بالاتر)؛ چون هر
// فایل آپلودی اسمش GUID یکتاست (هیچ‌وقت با همون اسم بازنویسی نمی‌شه)، کش طولانی و immutable
// کاملاً امنه
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/uploads",
    FileProvider = new PhysicalFileProvider(uploadsRoot),
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
    }
});

// بقیه فایل‌های استاتیک (js/css/html) از wwwroot سرو می‌شن؛ چون این پروژه هنوز نسخه‌بندی/hash
// تو اسم فایل نداره و مرتب در حال تغییره، کش کوتاه با اجبار به revalidate می‌گیرن - هم از کش
// مرورگر (با ۳۰۴ سریع) سود می‌بریم، هم هیچ‌وقت محتوای قدیمی برای چند روز/هفته گیر نمی‌کنه
// (دقیقاً همون مشکلی که موقع دیباگ کش شدن api.js دیدیم)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=3600, must-revalidate";
    }
});
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();

app.Run();

// -----------------------------------------------------------------------
// نکته: اگه می‌خوای موقع اجرای برنامه، Migration های جدید خودکار اعمال بشن
// (فقط برای محیط توسعه توصیه می‌شه، نه Production)، این بلوک رو قبل از app.Run()
// از حالت کامنت خارج کن:
//
// if (app.Environment.IsDevelopment())
// {
//     using var scope = app.Services.CreateScope();
//     var db = scope.ServiceProvider.GetRequiredService<Atelier.Infrastructure.Data.AppDbContext>();
//     db.Database.Migrate();
// }
// -----------------------------------------------------------------------
