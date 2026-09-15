using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Librus.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddBookCover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "cover_id",
                table: "books",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cover_id",
                table: "books");
        }
    }
}
