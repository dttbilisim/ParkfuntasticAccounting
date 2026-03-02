using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchIdToProductDiscountSellerBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Sellers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Product",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Discounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "Banners",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Sellers");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Product");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Discounts");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "Banners");
        }
    }
}
