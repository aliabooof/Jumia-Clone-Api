using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jumia_Api.Infrastructure.Context.Migrations
{
    /// <inheritdoc />
    public partial class ColumnSellerImage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Sellers",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Sellers");
        }
    }
}
