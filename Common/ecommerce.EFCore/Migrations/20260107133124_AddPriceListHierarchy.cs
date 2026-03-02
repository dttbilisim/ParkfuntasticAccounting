using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddPriceListHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "PriceLists",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CorporationId",
                table: "PriceLists",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "PriceLists");

            migrationBuilder.DropColumn(
                name: "CorporationId",
                table: "PriceLists");
        }
    }
}
