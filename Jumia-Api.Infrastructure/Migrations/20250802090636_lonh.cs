using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Jumia_Api.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class lonh : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
        name: "PK_CampaignJobRequests",
        table: "CampaignJobRequests");

            migrationBuilder.AlterColumn<long>(
                name: "Id",
                table: "CampaignJobRequests",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
        name: "PK_CampaignJobRequests",
        table: "CampaignJobRequests",
        column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "CampaignJobRequests",
                type: "int",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint")
                .Annotation("SqlServer:Identity", "1, 1")
                .OldAnnotation("SqlServer:Identity", "1, 1");
        }
    }
}
