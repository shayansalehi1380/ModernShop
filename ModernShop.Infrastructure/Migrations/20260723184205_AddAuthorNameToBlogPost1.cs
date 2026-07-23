using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ModernShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthorNameToBlogPost1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthorName",
                table: "BlogPosts",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthorName",
                table: "BlogPosts");
        }
    }
}
