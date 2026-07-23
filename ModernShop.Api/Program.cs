using System.Text;
using ModernShop.Api.Services;
using ModernShop.Core.Interfaces;
using ModernShop.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== لایه Infrastructure: DbContext + سرویس پیامک + سرویس درگاه پرداخت =====
builder.Services.AddInfrastructure(builder.Configuration);

// ===== سرویس‌های مخصوص همین لایه Api =====
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddScoped<JwtTokenService>();
builder.Services.Configure<AdminSettings>(builder.Configuration.GetSection("Admin"));

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
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
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
