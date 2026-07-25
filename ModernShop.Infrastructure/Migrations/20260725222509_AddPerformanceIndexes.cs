using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "GuestSessionId",
                table: "Carts",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_CreatedAt",
                table: "Products",
                columns: new[] { "IsActive", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Carts_GuestSessionId",
                table: "Carts",
                column: "GuestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_IsPublished_PublishedAt",
                table: "BlogPosts",
                columns: new[] { "IsPublished", "PublishedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_CreatedAt",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Carts_GuestSessionId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_BlogPosts_IsPublished_PublishedAt",
                table: "BlogPosts");

            migrationBuilder.AlterColumn<string>(
                name: "GuestSessionId",
                table: "Carts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(64)",
                oldMaxLength: 64,
                oldNullable: true);
        }
    }
}
