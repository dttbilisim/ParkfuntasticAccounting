using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class RemoveProductCodeAndRenamePurchasePriceToCostPrice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "PriceListItems");

            migrationBuilder.RenameColumn(
                name: "PurchasePrice",
                table: "PriceListItems",
                newName: "CostPrice");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CostPrice",
                table: "PriceListItems",
                newName: "PurchasePrice");

            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "PriceListItems",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
