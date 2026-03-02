using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class Phase2DefBranchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Tiers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Tiers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Surveys",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Surveys",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "ProductType",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "ProductType",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Brand",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "Brand",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Tiers");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Tiers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Surveys");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "ProductType");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "ProductType");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Brand");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "Brand");
        }
    }
}
