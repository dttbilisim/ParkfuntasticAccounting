using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ecommerce.EFCore.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAddressToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserAddressId",
                table: "Orders",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserAddressId",
                table: "Orders",
                column: "UserAddressId");

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_UserAddresses_UserAddressId",
                table: "Orders",
                column: "UserAddressId",
                principalTable: "UserAddresses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Orders_UserAddresses_UserAddressId",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_UserAddressId",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "UserAddressId",
                table: "Orders");
        }
    }
}
