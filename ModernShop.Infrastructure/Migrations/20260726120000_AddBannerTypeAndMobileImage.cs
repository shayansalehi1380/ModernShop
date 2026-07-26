using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBannerTypeAndMobileImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MobileImageUrl",
                table: "Banners",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Banners",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MobileImageUrl",
                table: "Banners");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Banners");
        }
    }
}
