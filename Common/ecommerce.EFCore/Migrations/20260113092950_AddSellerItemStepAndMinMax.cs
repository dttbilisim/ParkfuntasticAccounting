using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerItemStepAndMinMax : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Step",
                table: "SellerItems",
                type: "numeric",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "MinSaleAmount",
                table: "SellerItems",
                type: "numeric",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxSaleAmount",
                table: "SellerItems",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Step",
                table: "SellerItems");

            migrationBuilder.DropColumn(
                name: "MinSaleAmount",
                table: "SellerItems");

            migrationBuilder.DropColumn(
                name: "MaxSaleAmount",
                table: "SellerItems");
        }
    }
}
