using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminReplyToReviews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdminReply",
                table: "ProductReviews",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdminReply",
                table: "ProductReviews");
        }
    }
}
