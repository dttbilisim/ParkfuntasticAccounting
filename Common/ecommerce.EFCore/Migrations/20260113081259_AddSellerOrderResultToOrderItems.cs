using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddSellerOrderResultToOrderItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSellerBasketStatus",
                table: "OrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSellerOrderStatus",
                table: "OrderItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SellerOrderResult",
                table: "OrderItems",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSellerBasketStatus",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "IsSellerOrderStatus",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SellerOrderResult",
                table: "OrderItems");
        }
    }
}
