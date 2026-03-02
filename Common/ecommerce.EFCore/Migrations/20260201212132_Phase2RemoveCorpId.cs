using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class Phase2RemoveCorpId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Tiers");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "ProductType");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Brand");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Tiers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Surveys",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "ProductType",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Brand",
                type: "integer",
                nullable: true);
        }
    }
}
