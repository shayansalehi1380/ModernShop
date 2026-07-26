using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCartItemReservation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ردیف‌های موجود (سبدهای فعلی کاربرها) یک بازه‌ی تازه‌ی ۲ ساعته می‌گیرن تا همین الان
            // بی‌دلیل منقضی حساب نشن.
            migrationBuilder.AddColumn<DateTime>(
                name: "ReservedUntil",
                table: "CartItems",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(HOUR, 2, GETUTCDATE())");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId_ProductVariantId_ReservedUntil",
                table: "CartItems",
                columns: new[] { "ProductId", "ProductVariantId", "ReservedUntil" });

            // برای اینکه موقع ثبت سفارش موجودی همون تنوع انتخاب‌شده (نه فقط کل محصول) کم بشه؛
            // سفارش‌های قبلی (قبل این migration) چون از تنوع خاصی خبر نداشتن NULL می‌مونن.
            migrationBuilder.AddColumn<int>(
                name: "ProductVariantId",
                table: "OrderItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductVariantId",
                table: "OrderItems",
                column: "ProductVariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ProductVariants_ProductVariantId",
                table: "OrderItems",
                column: "ProductVariantId",
                principalTable: "ProductVariants",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ProductVariants_ProductVariantId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ProductVariantId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_CartItems_ProductId_ProductVariantId_ReservedUntil",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "ReservedUntil",
                table: "CartItems");
        }
    }
}
