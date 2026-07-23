using ModernShop.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ModernShop.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductSpecification> ProductSpecifications => Set<ProductSpecification>();
    public DbSet<ProductReview> ProductReviews => Set<ProductReview>();
    public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<DiscountCode> DiscountCodes => Set<DiscountCode>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();
    public DbSet<BlogPost> BlogPosts => Set<BlogPost>();
    public DbSet<BlogComment> BlogComments => Set<BlogComment>();
    public DbSet<NewsletterSubscriber> NewsletterSubscribers => Set<NewsletterSubscriber>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<StaticPage> StaticPages => Set<StaticPage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureDecimalPrecision(modelBuilder);
        ConfigureUniqueIndexes(modelBuilder);
        ConfigureDeleteBehavior(modelBuilder);
    }

    /// <summary>
    /// تومان اعشار نداره، پس همه‌ی فیلدهای decimal (قیمت، مبلغ تخفیف، هزینه ارسال و ...)
    /// رو یکجا decimal(18,0) می‌کنیم تا لازم نباشه رو تک‌تکشون [Column] بذاریم.
    /// </summary>
    private static void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        var decimalProperties = modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

        foreach (var property in decimalProperties)
        {
            property.SetColumnType("decimal(18,0)");
        }
    }

    private static void ConfigureUniqueIndexes(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(x => x.PhoneNumber).IsUnique();
        modelBuilder.Entity<Product>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<Category>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<BlogPost>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<BlogCategory>().HasIndex(x => x.Slug).IsUnique();
        modelBuilder.Entity<Order>().HasIndex(x => x.OrderNumber).IsUnique();
        modelBuilder.Entity<DiscountCode>().HasIndex(x => x.Code).IsUnique();
        modelBuilder.Entity<AppSetting>().HasIndex(x => x.Key).IsUnique();
        modelBuilder.Entity<NewsletterSubscriber>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<StaticPage>().HasIndex(x => x.Slug).IsUnique();

        // یک کاربر نمی‌تونه یک محصول رو دوبار به علاقه‌مندی‌ها اضافه کنه
        modelBuilder.Entity<WishlistItem>().HasIndex(x => new { x.UserId, x.ProductId }).IsUnique();
    }

    /// <summary>
    /// پیش‌فرض همه‌ی رابطه‌ها روی Restrict تنظیم می‌شن (نه حذف کاسکید)، به دو دلیل:
    ///  ۱) SQL Server با چند مسیر کاسکید هم‌زمان به یک جدول ارور می‌ده (Multiple Cascade Paths) —
    ///     با این‌همه رابطه به User و Product، این خیلی زود پیش میاد.
    ///  ۲) تو یک فروشگاه واقعی نباید با حذف یک کاربر یا محصول، سابقه سفارش‌ها و نظرات هم پاک بشه؛
    ///     به‌جاش از IsActive برای غیرفعال‌کردن استفاده کن، نه حذف واقعی.
    ///
    /// فقط جدول‌هایی که کاملاً زیرمجموعه‌ی یک والد مشخص هستن و بدون اون والد معنی ندارن
    /// (مثل آیتم‌های یک سفارش) کاسکید می‌شن.
    /// </summary>
    private static void ConfigureDeleteBehavior(ModelBuilder modelBuilder)
    {
        foreach (var foreignKey in modelBuilder.Model.GetEntityTypes().SelectMany(e => e.GetForeignKeys()))
        {
            foreignKey.DeleteBehavior = DeleteBehavior.Restrict;
        }

        modelBuilder.Entity<OrderItem>()
            .HasOne(x => x.Order).WithMany(x => x.Items)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<OrderStatusHistory>()
            .HasOne(x => x.Order).WithMany(x => x.StatusHistory)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Payment>()
            .HasOne(x => x.Order).WithMany(x => x.Payments)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductImage>()
            .HasOne(x => x.Product).WithMany(x => x.Images)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductVariant>()
            .HasOne(x => x.Product).WithMany(x => x.Variants)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ProductSpecification>()
            .HasOne(x => x.Product).WithMany(x => x.Specifications)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CartItem>()
            .HasOne(x => x.Cart).WithMany(x => x.Items)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BlogComment>()
            .HasOne(x => x.BlogPost).WithMany(x => x.Comments)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WishlistItem>()
            .HasOne(x => x.User).WithMany(x => x.WishlistItems)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
