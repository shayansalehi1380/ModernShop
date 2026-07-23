using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIncludedInDiscountToVariant : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludedInDiscount",
                table: "ProductVariants",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludedInDiscount",
                table: "ProductVariants");
        }
    }
}
